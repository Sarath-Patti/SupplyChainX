using FluentAssertions;
using SupplyChainX.Infrastructure.Messaging.Kafka;
using Xunit;

namespace SupplyChainX.UnitTests.Messaging;

public class KafkaConsumerStatusServiceTests
{
    [Fact]
    public void Service_InitialState_ShouldBeDefault()
    {
        // Arrange & Act
        var service = new KafkaConsumerStatusService();
        var status = service.GetStatus();

        // Assert
        status.IsRunning.Should().BeFalse();
        status.ConsumerGroupId.Should().BeEmpty();
        status.SubscribedTopics.Should().BeEmpty();
        status.Metrics.ConsumedCount.Should().Be(0);
        status.Metrics.ProcessedCount.Should().Be(0);
        status.Metrics.DuplicateCount.Should().Be(0);
        status.Metrics.FailureCount.Should().Be(0);
        status.Metrics.RetryCount.Should().Be(0);
        status.Metrics.DlqCount.Should().Be(0);
        status.Metrics.MalformedCount.Should().Be(0);
    }

    [Fact]
    public void RecordingMetrics_ShouldAtomicallyUpdateCounters()
    {
        // Arrange
        var service = new KafkaConsumerStatusService();
        var eventId = Guid.NewGuid();

        // Act
        service.SetConsumerState(true, "test-group", new[] { "topic-1" });
        service.RecordConsumed("topic-1", eventId, "TestEvent");
        service.RecordRetry("topic-1", eventId, "TestEvent", 1);
        service.RecordRetry("topic-1", eventId, "TestEvent", 2);
        service.RecordFailure("topic-1", eventId, "TestEvent", new InvalidOperationException("Fail"));
        service.RecordDlqSuccess("topic-1", "topic-1.dlq", eventId, "TestEvent");
        service.RecordDuplicate("topic-1", eventId, "TestEvent");
        service.RecordMalformed("topic-1", 100);

        var status = service.GetStatus();

        // Assert
        status.IsRunning.Should().BeTrue();
        status.ConsumerGroupId.Should().Be("test-group");
        status.SubscribedTopics.Should().ContainSingle().Which.Should().Be("topic-1");

        status.Metrics.ConsumedCount.Should().Be(1);
        status.Metrics.RetryCount.Should().Be(2);
        status.Metrics.FailureCount.Should().Be(1);
        status.Metrics.DlqCount.Should().Be(1);
        status.Metrics.DlqSuccessCount.Should().Be(1);
        status.Metrics.DuplicateCount.Should().Be(1);
        status.Metrics.MalformedCount.Should().Be(1);

        status.LastEventConsumedAtUtc.Should().NotBeNull();
        status.LastProcessingFailureAtUtc.Should().NotBeNull();
        status.LastProcessingFailureReason.Should().Be("Fail");
    }
}
