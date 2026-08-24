using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SupplyChainX.Infrastructure.Persistence;
using SupplyChainX.Infrastructure.Services;
using Xunit;

namespace SupplyChainX.UnitTests.Services;

public class IdempotencyServiceTests : IDisposable
{
    private readonly SupplyChainXDbContext _dbContext;
    private readonly IdempotencyService _idempotencyService;

    public IdempotencyServiceTests()
    {
        var options = new DbContextOptionsBuilder<SupplyChainXDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new SupplyChainXDbContext(options);
        var logger = Substitute.For<ILogger<IdempotencyService>>();

        _idempotencyService = new IdempotencyService(_dbContext, logger);
    }

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }

    [Fact]
    public async Task HasBeenProcessedAsync_WithUnprocessedEventId_ShouldReturnFalse()
    {
        // Arrange
        var eventId = Guid.NewGuid();

        // Act
        var result = await _idempotencyService.HasBeenProcessedAsync(eventId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task MarkAsProcessedAsync_WithValidEvent_ShouldPersistAndMakeHasBeenProcessedReturnTrue()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var eventType = "ProductCreatedEvent";

        // Act
        await _idempotencyService.MarkAsProcessedAsync(eventId, eventType);
        var result = await _idempotencyService.HasBeenProcessedAsync(eventId);

        // Assert
        result.Should().BeTrue();

        var dbRecord = await _dbContext.ProcessedEvents.FirstOrDefaultAsync(e => e.EventId == eventId);
        dbRecord.Should().NotBeNull();
        dbRecord!.EventType.Should().Be(eventType);
    }

    [Fact]
    public async Task MarkAsProcessedAsync_WithEmptyEventId_ShouldThrowArgumentException()
    {
        // Arrange
        var emptyEventId = Guid.Empty;

        // Act
        Func<Task> act = async () => await _idempotencyService.MarkAsProcessedAsync(emptyEventId, "TestEvent");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*EventId cannot be empty*");
    }
}
