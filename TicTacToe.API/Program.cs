using System.Text.Json.Serialization;
using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using TicTacToe.Application;
using TicTacToe.Configurations;
using TicTacToe.Infrastructure;
using TicTacToe.Infrastructure.Persistence;
using TicTacToe.Middleware;

public class Program
{
    async static Task Main(string[] args)
    {
        Env.Load();
        var builder = WebApplication.CreateBuilder(args);

        // Добавляем политику CORS
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowFrontend",
                builder =>
                {
                    // Укажите здесь URL вашего frontend-приложения
                    builder//.WithOrigins("http://localhost:3000", "http://localhost","http://37.128.206.147",
                            //"http://localhost:80")
                            .AllowAnyOrigin()
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .WithExposedHeaders("ETag");
                });
        });


        builder.Services.AddApplicationServices();
        builder.Services.AddInfrastructureServices(builder.Configuration, builder.Environment);
        builder.Services.AddSingleton<GameConfiguration>();


        builder.Services.AddControllers()
            .AddJsonOptions(options =>
            {
                // Эта строка заставляет .NET превращать Enum в строки (например, "InProgress" вместо 0)
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();


        var app = builder.Build();

        if (!app.Environment.IsEnvironment("Test"))
        {
            //применяем миграции
            await ApplyMigrationsAsync(app.Services);
        }

        app.UseMiddleware<ExceptionHandlingMiddleware>();


        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "UserService V1");
            options.RoutePrefix = string.Empty;
        });
        app.UseCors("AllowFrontend");

        app.MapControllers();


        app.Run();


        static async Task ApplyMigrationsAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();

            //получаем из DI context
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();


            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

            try
            {
                logger.LogInformation("Applying database migrations...");

                await dbContext.Database.MigrateAsync();

                logger.LogInformation("Database migrations applied successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while applying database migrations.");
                throw;
            }
        }
    }
}