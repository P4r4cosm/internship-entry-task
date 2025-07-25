using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TicTacToe.Application.Interfaces;
using TicTacToe.Infrastructure.Persistence;
using TicTacToe.Infrastructure.Persistence.Repositories;

namespace TicTacToe.Infrastructure;


public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        
        var host = configuration["DB_HOST"];
        var port = configuration["DB_PORT"];
        var user = configuration["POSTGRES_USER"];
        var password = configuration["POSTGRES_PASSWORD"];
        var dbName = configuration["POSTGRES_DB"];
        
        var connectionString = $"Host={host};Port={port};Username={user};Password={password};Database={dbName};";
        
        // Проверка, что строка не пустая 
        if (string.IsNullOrEmpty(connectionString) || host == null)
        {
            throw new InvalidOperationException("Database connection string is not configured.");
        }
        
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString,
                builder => builder.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));
        
        services.AddScoped<IGameRepository, GameRepository>();

        return services;
    }
}