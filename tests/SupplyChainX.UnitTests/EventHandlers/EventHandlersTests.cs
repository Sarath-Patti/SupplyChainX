using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SupplyChainX.Application.Common.Events;
using SupplyChainX.Application.EventHandlers;
using Xunit;

namespace SupplyChainX.UnitTests.EventHandlers;

public class EventHandlersTests
{
    [Fact]
    public async Task ProductCreatedEventHandler_WithValidEvent_ShouldProcessSuccessfully()
    {
        // Arrange
        var logger = Substitute.For<ILogger<ProductCreatedEventHandler>>();
        var handler = new ProductCreatedEventHandler(logger);
        var @event = new ProductCreatedEvent(Guid.NewGuid(), DateTime.UtcNow, Guid.NewGuid(), "SKU-H1", "Widget H1", "Desc", 15.00m, true);

        // Act
        Func<Task> act = async () => await handler.HandleAsync(@event);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ProductCreatedEventHandler_WithNullEvent_ShouldThrowArgumentNullException()
    {
        // Arrange
        var logger = Substitute.For<ILogger<ProductCreatedEventHandler>>();
        var handler = new ProductCreatedEventHandler(logger);

        // Act
        Func<Task> act = async () => await handler.HandleAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ProductUpdatedEventHandler_WithValidEvent_ShouldProcessSuccessfully()
    {
        // Arrange
        var logger = Substitute.For<ILogger<ProductUpdatedEventHandler>>();
        var handler = new ProductUpdatedEventHandler(logger);
        var @event = new ProductUpdatedEvent(Guid.NewGuid(), DateTime.UtcNow, Guid.NewGuid(), "SKU-H2", "Updated H2", "Desc", 25.00m, true);

        // Act
        Func<Task> act = async () => await handler.HandleAsync(@event);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ProductDeletedEventHandler_WithValidEvent_ShouldProcessSuccessfully()
    {
        // Arrange
        var logger = Substitute.For<ILogger<ProductDeletedEventHandler>>();
        var handler = new ProductDeletedEventHandler(logger);
        var @event = new ProductDeletedEvent(Guid.NewGuid(), DateTime.UtcNow, Guid.NewGuid(), "SKU-DEL");

        // Act
        Func<Task> act = async () => await handler.HandleAsync(@event);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task WarehouseCreatedEventHandler_WithValidEvent_ShouldProcessSuccessfully()
    {
        // Arrange
        var logger = Substitute.For<ILogger<WarehouseCreatedEventHandler>>();
        var handler = new WarehouseCreatedEventHandler(logger);
        var @event = new WarehouseCreatedEvent(Guid.NewGuid(), DateTime.UtcNow, Guid.NewGuid(), "Warehouse Alpha", "Chicago", true);

        // Act
        Func<Task> act = async () => await handler.HandleAsync(@event);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task WarehouseUpdatedEventHandler_WithValidEvent_ShouldProcessSuccessfully()
    {
        // Arrange
        var logger = Substitute.For<ILogger<WarehouseUpdatedEventHandler>>();
        var handler = new WarehouseUpdatedEventHandler(logger);
        var @event = new WarehouseUpdatedEvent(Guid.NewGuid(), DateTime.UtcNow, Guid.NewGuid(), "Warehouse Beta", "Austin", true);

        // Act
        Func<Task> act = async () => await handler.HandleAsync(@event);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task WarehouseDeletedEventHandler_WithValidEvent_ShouldProcessSuccessfully()
    {
        // Arrange
        var logger = Substitute.For<ILogger<WarehouseDeletedEventHandler>>();
        var handler = new WarehouseDeletedEventHandler(logger);
        var @event = new WarehouseDeletedEvent(Guid.NewGuid(), DateTime.UtcNow, Guid.NewGuid(), "Warehouse Gamma");

        // Act
        Func<Task> act = async () => await handler.HandleAsync(@event);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task InventoryAdjustedEventHandler_WithValidEvent_ShouldProcessSuccessfully()
    {
        // Arrange
        var logger = Substitute.For<ILogger<InventoryAdjustedEventHandler>>();
        var handler = new InventoryAdjustedEventHandler(logger);
        var @event = new InventoryAdjustedEvent(
            Guid.NewGuid(), DateTime.UtcNow, Guid.NewGuid(), Guid.NewGuid(), "SKU-H3", Guid.NewGuid(), "Warehouse Delta", 100, 0, 100, "Increase", 1);

        // Act
        Func<Task> act = async () => await handler.HandleAsync(@event);

        // Assert
        await act.Should().NotThrowAsync();
    }
}
