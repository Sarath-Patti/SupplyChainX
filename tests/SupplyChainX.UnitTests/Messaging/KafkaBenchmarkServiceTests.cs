using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using SupplyChainX.Application.Common.Configuration;
using SupplyChainX.Application.Common.Interfaces;
using SupplyChainX.Application.Common.Models;
using SupplyChainX.Infrastructure.Messaging.Kafka;
using Xunit;

namespace SupplyChainX.UnitTests.Messaging;

public class KafkaBenchmarkServiceTests
{
    private readonly IEventPublisher _publisher;
    private readonly IKafkaConsumerStatusService _statusService;
    private readonly IOptions<KafkaTopicOptions> _topicOptions;
    private readonly IOptions<KafkaConsumerOptions> _consumerOptions;
    private readonly IConfiguration _configuration;
    private readonly ILogger<KafkaBenchmarkService> _logger;
    private readonly KafkaBenchmarkService _benchmarkService;

    public KafkaBenchmarkServiceTests()
    {
        _publisher = Substitute.For<IEventPublisher>();
        _statusService = Substitute.For<IKafkaConsumerStatusService>();
        _logger = Substitute.For<ILogger<KafkaBenchmarkService>>();

        _topicOptions = Options.Create(new KafkaTopicOptions
        {
            ProductEvents = "supplychainx.product.events",
            WarehouseEvents = "supplychainx.warehouse.events",
            InventoryEvents = "supplychainx.inventory.events"
        });

        _consumerOptions = Options.Create(new KafkaConsumerOptions
        {
            ConsumerGroupId = "test-consumer-group",
            AutoOffsetReset = "Earliest"
        });

        var inMemorySettings = new Dictionary<string, string?>
        {
            { "Kafka:BootstrapServers", "localhost:9092" }
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        _statusService.GetStatus().Returns(new KafkaConsumerStatusDto
        {
            IsRunning = true,
            Metrics = new KafkaMetricsDto
            {
                ConsumedCount = 10,
                ProcessedCount = 10,
                FailureCount = 0
            }
        });

        _benchmarkService = new KafkaBenchmarkService(
            _publisher,
            _statusService,
            _topicOptions,
            _consumerOptions,
            _configuration,
            _logger);
    }

    [Fact]
    public async Task PublishWorkloadAsync_WithValidRequest_ShouldPublishEventsAcrossTopics()
    {
        // Arrange
        var request = new BenchmarkWorkloadRequest
        {
            EventCount = 6,
            DelayBetweenEventsMs = 0
        };

        // Act
        var result = await _benchmarkService.PublishWorkloadAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.WorkloadType.Should().Be("StandardWorkload");
        result.EventsProduced.Should().Be(6);
        result.TotalDurationSeconds.Should().BeGreaterThanOrEqualTo(0);

        // Verify publishers were called 6 times total across the topics
        await _publisher.ReceivedWithAnyArgs(6).PublishAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<object>());
    }

    [Fact]
    public async Task TriggerBurstWorkloadAsync_WithValidRequest_ShouldPublishBurstEvents()
    {
        // Arrange
        var request = new BenchmarkBurstRequest
        {
            BurstEventCount = 9
        };

        // Act
        var result = await _benchmarkService.TriggerBurstWorkloadAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.WorkloadType.Should().Be("BackpressureBurst");
        result.EventsProduced.Should().Be(9);

        // Verify 9 event publish calls
        await _publisher.ReceivedWithAnyArgs(9).PublishAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<object>());
    }
}
