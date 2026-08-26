using System.Text;
using Confluent.Kafka;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using SupplyChainX.Application.Common.Configuration;
using SupplyChainX.Application.Common.Interfaces;
using SupplyChainX.Application.Services;
using SupplyChainX.Domain.Entities;
using SupplyChainX.Infrastructure.Health;
using SupplyChainX.Infrastructure.Messaging.Kafka;
using SupplyChainX.Infrastructure.Persistence;
using SupplyChainX.Infrastructure.Services;

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
            ?? "Host=localhost;Port=5433;Database=supplychainx_db;Username=postgres;Password=postgres_dev_password";

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
        services.AddScoped<IIdempotencyService, IdempotencyService>();

        // 2. Auth & JWT Configuration
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        var jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();

        services.AddSingleton<IPasswordService, PasswordService>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IAuthService, AuthService>();

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = false;
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey)),
                ValidateIssuer = true,
                ValidIssuer = jwtOptions.Issuer,
                ValidateAudience = true,
                ValidAudience = jwtOptions.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
        });

        services.AddAuthorization(options =>
        {
            options.AddPolicy("AdminOnly", policy => policy.RequireRole(Role.Admin));
            options.AddPolicy("OperatorOrAdmin", policy => policy.RequireRole(Role.Admin, Role.Operator));
            options.AddPolicy("ViewerOrHigher", policy => policy.RequireRole(Role.Admin, Role.Operator, Role.Viewer));
        });

        // 3. Status & Observability Services
        services.AddSingleton<IKafkaConsumerStatusService, KafkaConsumerStatusService>();

        // 4. Health Checks
        services.AddHealthChecks()
            .AddDbContextCheck<SupplyChainXDbContext>(
                name: "database",
                tags: new[] { "db", "postgresql", "ready" })
            .AddCheck<KafkaHealthCheck>(
                name: "kafka",
                tags: new[] { "messaging", "kafka", "ready" });

        // 5. Kafka Producer Messaging Configuration
        var bootstrapServers = configuration["Kafka:BootstrapServers"]
            ?? Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS")
            ?? "localhost:9092";

        services.Configure<KafkaTopicOptions>(configuration.GetSection(KafkaTopicOptions.SectionName));
        services.Configure<KafkaConsumerOptions>(configuration.GetSection(KafkaConsumerOptions.SectionName));
        services.Configure<KafkaRetryOptions>(configuration.GetSection(KafkaRetryOptions.SectionName));

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

        // 6. Kafka Consumer Hosted Service
        services.AddHostedService<KafkaConsumerBackgroundService>();

        return services;
    }
}
