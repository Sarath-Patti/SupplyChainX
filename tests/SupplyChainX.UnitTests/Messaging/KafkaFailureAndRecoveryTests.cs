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

public class KafkaFailureAndRecoveryTests
{
    private readonly IEventPublisher _publisher;
    private readonly IKafkaConsumerStatusService _statusService;
    private readonly IOptions<KafkaTopicOptions> _topicOptions;
    private readonly IOptions<KafkaConsumerOptions> _consumerOptions;
    private readonly IConfiguration _configuration;
    private readonly ILogger<KafkaBenchmarkService> _logger;
    private readonly KafkaBenchmarkService _benchmarkService;

    public KafkaFailureAndRecoveryTests()
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
                ConsumedCount = 5,
                ProcessedCount = 5,
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
    public async Task PublishDuplicateEventAsync_ShouldPublishExactSameEventTwice()
    {
        // Act
        var result = await _benchmarkService.PublishDuplicateEventAsync();

        // Assert
        result.Should().NotBeNull();
        result.WorkloadType.Should().Be("DuplicateEventValidation");
        result.EventsProduced.Should().Be(2);

        // Verify publisher produced 2 messages
        await _publisher.ReceivedWithAnyArgs(2).PublishAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<object>());
    }

    [Fact]
    public async Task PublishPoisonEventAsync_ShouldPublishRawPoisonPayload()
    {
        // Act
        var result = await _benchmarkService.PublishPoisonEventAsync();

        // Assert
        result.Should().NotBeNull();
        result.WorkloadType.Should().Be("PoisonEventValidation");
        result.EventsProduced.Should().Be(1);

        // Verify raw publish call
        await _publisher.ReceivedWithAnyArgs(1).PublishRawAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IDictionary<string, string>>());
    }
}
