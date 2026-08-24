using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NSubstitute;
using SupplyChainX.Application.Common.Configuration;
using SupplyChainX.Application.Common.Events;
using SupplyChainX.Application.Common.Interfaces;
using SupplyChainX.Application.DTOs;
using SupplyChainX.Application.Services;
using SupplyChainX.Domain.Entities;
using SupplyChainX.Domain.Exceptions;
using SupplyChainX.Infrastructure.Persistence;
using Xunit;

namespace SupplyChainX.UnitTests.Services;

public class InventoryServiceTests : IDisposable
{
    private readonly SupplyChainXDbContext _dbContext;
    private readonly IEventPublisher _eventPublisher;
    private readonly IOptions<KafkaTopicOptions> _topicOptions;
    private readonly InventoryService _service;
    private readonly Product _product;
    private readonly Warehouse _warehouse;

    public InventoryServiceTests()
    {
        var options = new DbContextOptionsBuilder<SupplyChainXDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new SupplyChainXDbContext(options);
        _eventPublisher = Substitute.For<IEventPublisher>();
        _topicOptions = Options.Create(new KafkaTopicOptions
        {
            ProductEvents = "supplychainx.product.events",
            WarehouseEvents = "supplychainx.warehouse.events",
            InventoryEvents = "supplychainx.inventory.events"
        });

        _service = new InventoryService(_dbContext, _eventPublisher, _topicOptions);

        _product = new Product("SKU-TEST", "Test Product", "Desc", 99.99m);
        _warehouse = new Warehouse("Main Warehouse", "Building A");

        _dbContext.Products.Add(_product);
        _dbContext.Warehouses.Add(_warehouse);
        _dbContext.SaveChanges();
    }

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }

    [Fact]
    public async Task AdjustInventoryAsync_IncreaseStock_ShouldCreateAndIncreaseInventoryAndPublishEvent()
    {
        // Arrange
        var request = new AdjustInventoryRequest(_product.Id, _warehouse.Id, 100, InventoryAdjustmentType.Increase);

        // Act
        var result = await _service.AdjustInventoryAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.AvailableQuantity.Should().Be(100);
        result.ReservedQuantity.Should().Be(0);
        result.ProductId.Should().Be(_product.Id);
        result.WarehouseId.Should().Be(_warehouse.Id);

        // Verify Event Publishing
        await _eventPublisher.Received(1).PublishAsync(
            _topicOptions.Value.InventoryEvents,
            result.Id.ToString(),
            Arg.Is<InventoryAdjustedEvent>(e => e.InventoryId == result.Id && e.AvailableQuantity == 100 && e.AdjustmentType == "Increase"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AdjustInventoryAsync_DecreaseStock_ShouldReduceAvailableStockAndPublishEvent()
    {
        // Arrange
        await _service.AdjustInventoryAsync(new AdjustInventoryRequest(_product.Id, _warehouse.Id, 100, InventoryAdjustmentType.Increase));
        _eventPublisher.ClearReceivedCalls();

        // Act
        var result = await _service.AdjustInventoryAsync(new AdjustInventoryRequest(_product.Id, _warehouse.Id, 40, InventoryAdjustmentType.Decrease));

        // Assert
        result.AvailableQuantity.Should().Be(60);

        await _eventPublisher.Received(1).PublishAsync(
            _topicOptions.Value.InventoryEvents,
            result.Id.ToString(),
            Arg.Is<InventoryAdjustedEvent>(e => e.AvailableQuantity == 60 && e.AdjustmentType == "Decrease"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AdjustInventoryAsync_DecreaseStockExceedingAvailable_ShouldThrowDomainExceptionAndNotPublishEvent()
    {
        // Arrange
        await _service.AdjustInventoryAsync(new AdjustInventoryRequest(_product.Id, _warehouse.Id, 20, InventoryAdjustmentType.Increase));
        _eventPublisher.ClearReceivedCalls();

        // Act
        Func<Task> act = async () => await _service.AdjustInventoryAsync(new AdjustInventoryRequest(_product.Id, _warehouse.Id, 50, InventoryAdjustmentType.Decrease));

        // Assert
        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*Insufficient available stock*");

        await _eventPublisher.DidNotReceiveWithAnyArgs().PublishAsync<InventoryAdjustedEvent>(default!, default!, default!, default);
    }

    [Fact]
    public async Task AdjustInventoryAsync_ReserveStockExceedingAvailable_ShouldThrowDomainExceptionAndNotPublishEvent()
    {
        // Arrange
        await _service.AdjustInventoryAsync(new AdjustInventoryRequest(_product.Id, _warehouse.Id, 30, InventoryAdjustmentType.Increase));
        _eventPublisher.ClearReceivedCalls();

        // Act
        Func<Task> act = async () => await _service.AdjustInventoryAsync(new AdjustInventoryRequest(_product.Id, _warehouse.Id, 50, InventoryAdjustmentType.Reserve));

        // Assert
        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*Cannot reserve stock exceeding available stock*");

        await _eventPublisher.DidNotReceiveWithAnyArgs().PublishAsync<InventoryAdjustedEvent>(default!, default!, default!, default);
    }

    [Fact]
    public async Task AdjustInventoryAsync_WithInvalidProductId_ShouldThrowNotFoundExceptionAndNotPublishEvent()
    {
        // Arrange
        var invalidProductId = Guid.NewGuid();
        var request = new AdjustInventoryRequest(invalidProductId, _warehouse.Id, 10, InventoryAdjustmentType.Increase);

        // Act
        Func<Task> act = async () => await _service.AdjustInventoryAsync(request);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"*Product*({invalidProductId})*was not found*");

        await _eventPublisher.DidNotReceiveWithAnyArgs().PublishAsync<InventoryAdjustedEvent>(default!, default!, default!, default);
    }
}
