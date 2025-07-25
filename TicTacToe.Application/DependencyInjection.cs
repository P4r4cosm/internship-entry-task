using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using FluentValidation;

namespace TicTacToe.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddAutoMapper((serviceProvider, config) => { config.AddMaps(Assembly.GetExecutingAssembly()); },
            Assembly.GetExecutingAssembly());
        
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));


        return services;
    }
}