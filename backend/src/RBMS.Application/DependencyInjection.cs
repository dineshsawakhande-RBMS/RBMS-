using System.Reflection;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using RBMS.Application.Common.Behaviors;
using RBMS.Application.Common.Interfaces;
using RBMS.Application.Common.Services;

namespace RBMS.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            // Order matters: log → validate → transaction → handler
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            cfg.AddOpenBehavior(typeof(UnitOfWorkBehavior<,>));
        });

        services.AddValidatorsFromAssembly(assembly);

        // The single choke-point for all stock changes (see IStockLedger).
        services.AddScoped<IStockLedger, StockLedger>();

        return services;
    }
}
