using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SupplyChainX.Application.Common.Configuration;
using SupplyChainX.Application.Common.Interfaces;
using SupplyChainX.Infrastructure.Messaging.Kafka;
using SupplyChainX.Infrastructure.Persistence;

namespace SupplyChainX.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // 1. PostgreSQL Database Configuration
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? Environment.GetEnvironmentVariable("POSTGRES_CONNECTION_STRING")
            ?? "Host=localhost;Port=5432;Database=supplychainx_db;Username=postgres;Password=postgres_dev_password";

        services.AddDbContext<SupplyChainXDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly(typeof(SupplyChainXDbContext).Assembly.FullName);
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorCodesToAdd: null);
            });
        });

        services.AddScoped<ISupplyChainXDbContext>(provider => provider.GetRequiredService<SupplyChainXDbContext>());

        // 2. Health Checks
        services.AddHealthChecks()
            .AddDbContextCheck<SupplyChainXDbContext>(
                name: "database",
                tags: new[] { "db", "postgresql" });

        // 3. Kafka Messaging Configuration
        var bootstrapServers = configuration["Kafka:BootstrapServers"]
            ?? Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS")
            ?? "localhost:9092";

        services.Configure<KafkaTopicOptions>(configuration.GetSection(KafkaTopicOptions.SectionName));

        services.AddSingleton<IProducer<string, string>>(sp =>
        {
            var producerConfig = new ProducerConfig
            {
                BootstrapServers = bootstrapServers,
                Acks = Acks.All,
                EnableDeliveryReports = true,
                MessageTimeoutMs = 5000,
                RequestTimeoutMs = 5000
            };

            return new ProducerBuilder<string, string>(producerConfig).Build();
        });

        services.AddScoped<IEventPublisher, KafkaEventPublisher>();

        return services;
    }
}
