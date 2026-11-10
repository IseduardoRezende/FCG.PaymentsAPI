using FCG.Payments.Application.Abstractions;
using FCG.Payments.Application.Services;
using FCG.Payments.Application.Services.Interfaces;
using FCG.Payments.Domain.Repositories;
using FCG.Payments.Domain.Settings;
using FCG.Payments.Infrastructure.DbContexts;
using FCG.Payments.Infrastructure.Messaging;
using FCG.Payments.Infrastructure.Messaging.Consumers;
using FCG.Payments.Infrastructure.Repositories;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FCG.Payments.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddDependencies(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<PaymentsDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("DbConnection"), sqlOptions =>
            {
                sqlOptions.MigrationsAssembly(typeof(PaymentsDbContext).Assembly.FullName);
                sqlOptions.EnableRetryOnFailure(maxRetryCount: 10, maxRetryDelay: TimeSpan.FromSeconds(30), errorCodesToAdd: null);
            });
        });

        services.AddHealthChecks().AddDbContextCheck<PaymentsDbContext>();

        services.Configure<PaymentSettings>(configuration.GetSection(PaymentSettings.SectionName));

        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IPaymentService, PaymentService>();

        services.AddMessaging(configuration);

        return services;
    }

    private static void AddMessaging(this IServiceCollection services, IConfiguration configuration)
    {
        var settings = configuration.GetSection(RabbitMqSettings.SectionName).Get<RabbitMqSettings>() ?? new RabbitMqSettings();

        services.AddScoped<IEventPublisher, MassTransitEventPublisher>();

        services.AddMassTransit(bus =>
        {
            bus.SetKebabCaseEndpointNameFormatter();

            bus.AddConsumer<OrderPlacedConsumer>()
                .Endpoint(endpoint => endpoint.Name = settings.OrderPlacedQueue);

            bus.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(settings.Host, settings.VirtualHost, host =>
                {
                    host.Username(settings.Username);
                    host.Password(settings.Password);
                });

                cfg.UseMessageRetry(retry => retry.Interval(3, TimeSpan.FromSeconds(5)));
                cfg.ConfigureEndpoints(context);
            });
        });
    }
}
