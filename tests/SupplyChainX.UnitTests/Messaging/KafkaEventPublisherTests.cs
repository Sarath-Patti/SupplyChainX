using Confluent.Kafka;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SupplyChainX.Application.Common.Events;
using SupplyChainX.Infrastructure.Messaging.Kafka;
using Xunit;

namespace SupplyChainX.UnitTests.Messaging;

public class KafkaEventPublisherTests
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaEventPublisher> _logger;

    public KafkaEventPublisherTests()
    {
        _producer = Substitute.For<IProducer<string, string>>();
        _logger = Substitute.For<ILogger<KafkaEventPublisher>>();
    }

    private KafkaEventPublisher CreatePublisher()
    {
        return new KafkaEventPublisher(_producer, _logger);
    }

    [Fact]
    public async Task PublishAsync_WithValidEvent_ShouldSerializeAndProduceToKafka()
    {
        // Arrange
        var publisher = CreatePublisher();
        var topic = "supplychainx.product.events";
        var key = "prod-123";
        var productEvent = new ProductCreatedEvent(
            Guid.NewGuid(),
            DateTime.UtcNow,
            Guid.NewGuid(),
            "SKU-TEST-1",
            "Test Product",
            "Description",
            99.99m,
            true);

        _producer.ProduceAsync(Arg.Any<string>(), Arg.Any<Message<string, string>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new DeliveryResult<string, string>
            {
                Topic = topic,
                Partition = new Partition(0),
                Offset = new Offset(10)
            }));

        // Act
        await publisher.PublishAsync(topic, key, productEvent);

        // Assert
        await _producer.Received(1).ProduceAsync(
            topic,
            Arg.Is<Message<string, string>>(m => m.Key == key && m.Value.Contains("SKU-TEST-1")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PublishAsync_WithNullTopic_ShouldThrowArgumentException()
    {
        // Arrange
        var publisher = CreatePublisher();
        var productEvent = new ProductCreatedEvent(
            Guid.NewGuid(), DateTime.UtcNow, Guid.NewGuid(), "SKU", "Name", null, 10m, true);

        // Act
        var act = async () => await publisher.PublishAsync("", "key", productEvent);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task PublishAsync_WithNullEvent_ShouldThrowArgumentNullException()
    {
        // Arrange
        var publisher = CreatePublisher();

        // Act
        var act = async () => await publisher.PublishAsync<ProductCreatedEvent>("topic", "key", null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task PublishRawAsync_WithHeaders_ShouldAttachHeadersAndProduceToKafka()
    {
        // Arrange
        var publisher = CreatePublisher();
        var topic = "supplychainx.product.events.dlq";
        var key = "key-dlq";
        var rawJson = "{\"eventId\":\"11111111-1111-1111-1111-111111111111\"}";
        var headers = new Dictionary<string, string>
        {
            ["x-original-topic"] = "supplychainx.product.events",
            ["x-exception-message"] = "Test failure"
        };

        _producer.ProduceAsync(Arg.Any<string>(), Arg.Any<Message<string, string>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new DeliveryResult<string, string>
            {
                Topic = topic,
                Partition = new Partition(1),
                Offset = new Offset(5)
            }));

        // Act
        await publisher.PublishRawAsync(topic, key, rawJson, headers);

        // Assert
        await _producer.Received(1).ProduceAsync(
            topic,
            Arg.Is<Message<string, string>>(m => m.Key == key && m.Value == rawJson && m.Headers.Count == 2),
            Arg.Any<CancellationToken>());
    }
}
