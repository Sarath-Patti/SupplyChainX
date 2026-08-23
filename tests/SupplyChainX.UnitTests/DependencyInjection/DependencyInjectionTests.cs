using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using SupplyChainX.Application;
using SupplyChainX.Application.Common.Interfaces;
using SupplyChainX.Infrastructure;
using SupplyChainX.Infrastructure.Persistence;
using Xunit;

namespace SupplyChainX.UnitTests.DependencyInjection;

public class DependencyInjectionTests
{
    [Fact]
    public void AddInfrastructure_ShouldRegisterDbContextAndHealthChecks()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ConnectionStrings:DefaultConnection", "Host=localhost;Database=test_db;Username=postgres;Password=postgres" }
            })
            .Build();

        // Act
        services.AddInfrastructure(configuration);
        var provider = services.BuildServiceProvider();

        // Assert
        provider.GetService<SupplyChainXDbContext>().Should().NotBeNull();
        provider.GetService<ISupplyChainXDbContext>().Should().NotBeNull();
        provider.GetService<HealthCheckService>().Should().NotBeNull();
    }

    [Fact]
    public void AddApplication_ShouldRegisterApplicationServicesWithoutExceptions()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddApplication();

        // Assert
        services.Should().NotBeEmpty();
    }
}
