using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TicTacToe.Configurations;
using TicTacToe.Infrastructure.Persistence;

namespace TicTacToe.Tests.IntegrationTests;

public class CustomWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");

        builder.ConfigureServices(services =>
        {
            // ШАГ 1: Находим и удаляем дескриптор сервиса для DbContext.
            // Это самый надежный способ удалить все, что было зарегистрировано через AddDbContext.
            services.AddOptions<GameConfiguration>();
            var dbContextDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));

            if (dbContextDescriptor != null)
            {
                services.Remove(dbContextDescriptor);
            }

            // ШАГ 2: Добавляем новую регистрацию с провайдером InMemory.
            // Теперь контейнер чист и готов к новой конфигурации.
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                // Используем постоянное имя, чтобы база данных была общей для всех тестов в одном запуске.
                // Использование Guid.NewGuid() создавало бы новую БД для каждого теста, что обычно нежелательно.
                options.UseInMemoryDatabase("InMemoryDbForTesting");
            });
            
            // ШАГ 3: Создаем ServiceProvider для инициализации БД.
            var sp = services.BuildServiceProvider();

            using (var scope = sp.CreateScope())
            {
                var scopedServices = scope.ServiceProvider;
                var db = scopedServices.GetRequiredService<ApplicationDbContext>();
                var logger = scopedServices
                    .GetRequiredService<ILogger<CustomWebApplicationFactory<TProgram>>>();

                // Убеждаемся, что база данных (в памяти) создана.
                db.Database.EnsureCreated();
                
            }
        });
    }
}