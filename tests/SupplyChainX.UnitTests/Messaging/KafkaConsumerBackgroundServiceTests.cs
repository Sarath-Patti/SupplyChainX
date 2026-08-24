using System.Text.Json;
using Confluent.Kafka;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using SupplyChainX.Application.Common.Configuration;
using SupplyChainX.Application.Common.Events;
using SupplyChainX.Application.Common.Interfaces;
using SupplyChainX.Infrastructure.Messaging.Kafka;
using Xunit;

namespace SupplyChainX.UnitTests.Messaging;

public class KafkaConsumerBackgroundServiceTests
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IServiceScope _scope;
    private readonly IServiceProvider _serviceProvider;
    private readonly IIdempotencyService _idempotencyService;
    private readonly IEventPublisher _eventPublisher;
    private readonly IEventHandler<ProductCreatedEvent> _productCreatedHandler;
    private readonly ILogger<KafkaConsumerBackgroundService> _logger;
    private readonly IOptions<KafkaConsumerOptions> _consumerOptions;
    private readonly IOptions<KafkaTopicOptions> _topicOptions;
    private readonly Microsoft.Extensions.Configuration.IConfiguration _configuration;

    public KafkaConsumerBackgroundServiceTests()
    {
        _scopeFactory = Substitute.For<IServiceScopeFactory>();
        _scope = Substitute.For<IServiceScope>();
        _serviceProvider = Substitute.For<IServiceProvider>();
        _idempotencyService = Substitute.For<IIdempotencyService>();
        _eventPublisher = Substitute.For<IEventPublisher>();
        _productCreatedHandler = Substitute.For<IEventHandler<ProductCreatedEvent>>();
        _logger = Substitute.For<ILogger<KafkaConsumerBackgroundService>>();
        _configuration = Substitute.For<Microsoft.Extensions.Configuration.IConfiguration>();

        _scopeFactory.CreateScope().Returns(_scope);
        _scope.ServiceProvider.Returns(_serviceProvider);

        _serviceProvider.GetService(typeof(IIdempotencyService)).Returns(_idempotencyService);
        _serviceProvider.GetService(typeof(IEventPublisher)).Returns(_eventPublisher);
        _serviceProvider.GetService(typeof(IEventHandler<ProductCreatedEvent>)).Returns(_productCreatedHandler);

        _consumerOptions = Microsoft.Extensions.Options.Options.Create(new KafkaConsumerOptions
        {
            ConsumerGroupId = "test-group",
            MaxRetryAttempts = 3,
            RetryDelayMs = 10,
            UseExponentialBackoff = false
        });

        _topicOptions = Microsoft.Extensions.Options.Options.Create(new KafkaTopicOptions
        {
            ProductEvents = "supplychainx.product.events",
            ProductEventsDlq = "supplychainx.product.events.dlq"
        });
    }

    private KafkaConsumerBackgroundService CreateService()
    {
        return new KafkaConsumerBackgroundService(
            _scopeFactory,
            _consumerOptions,
            _topicOptions,
            _configuration,
            _logger);
    }

    private static ConsumeResult<string, string> CreateConsumeResult(string topic, string rawJson, int partition = 0, long offset = 0)
    {
        return new ConsumeResult<string, string>
        {
            Topic = topic,
            Partition = new Partition(partition),
            Offset = new Offset(offset),
            Message = new Message<string, string>
            {
                Key = "test-key",
                Value = rawJson
            }
        };
    }

    [Fact]
    public void ProcessMessage_TransientFailure_ShouldRetryAndSucceed()
    {
        // Arrange
        var service = CreateService();
        var eventId = Guid.NewGuid();
        var rawJson = JsonSerializer.Serialize(new
        {
            eventId = eventId,
            eventType = nameof(ProductCreatedEvent),
            productId = Guid.NewGuid(),
            sku = "SKU-RETRY-01",
            name = "Retry Test",
            unitPrice = 10.0,
            isActive = true
        });

        var consumeResult = CreateConsumeResult("supplychainx.product.events", rawJson);
        var consumer = Substitute.For<IConsumer<string, string>>();

        _idempotencyService.HasBeenProcessedAsync(eventId, Arg.Any<CancellationToken>()).Returns(false);

        var calls = 0;
        _productCreatedHandler.HandleAsync(Arg.Any<ProductCreatedEvent>(), Arg.Any<CancellationToken>())
            .Returns(x =>
            {
                calls++;
                if (calls == 1) throw new InvalidOperationException("Transient DB failure");
                return Task.CompletedTask;
            });

        // Act
        service.ProcessMessage(consumer, consumeResult, CancellationToken.None);

        // Assert
        calls.Should().Be(2);
        _idempotencyService.Received(1).MarkAsProcessedAsync(eventId, nameof(ProductCreatedEvent), Arg.Any<CancellationToken>());
        consumer.Received(1).Commit(consumeResult);
        _eventPublisher.DidNotReceiveWithAnyArgs().PublishRawAsync(default!, default!, default!, default, default);
    }

    [Fact]
    public void ProcessMessage_SuccessfulProcessingAfterRetry_ShouldMarkProcessedAndCommitOffset()
    {
        // Arrange
        var service = CreateService();
        var eventId = Guid.NewGuid();
        var rawJson = JsonSerializer.Serialize(new
        {
            eventId = eventId,
            eventType = nameof(ProductCreatedEvent),
            productId = Guid.NewGuid(),
            sku = "SKU-RETRY-02",
            name = "Retry Test 2",
            unitPrice = 20.0,
            isActive = true
        });

        var consumeResult = CreateConsumeResult("supplychainx.product.events", rawJson);
        var consumer = Substitute.For<IConsumer<string, string>>();

        _idempotencyService.HasBeenProcessedAsync(eventId, Arg.Any<CancellationToken>()).Returns(false);

        var calls = 0;
        _productCreatedHandler.HandleAsync(Arg.Any<ProductCreatedEvent>(), Arg.Any<CancellationToken>())
            .Returns(x =>
            {
                calls++;
                if (calls < 3) throw new TimeoutException("Network timeout");
                return Task.CompletedTask;
            });

        // Act
        service.ProcessMessage(consumer, consumeResult, CancellationToken.None);

        // Assert
        calls.Should().Be(3);
        _idempotencyService.Received(1).MarkAsProcessedAsync(eventId, nameof(ProductCreatedEvent), Arg.Any<CancellationToken>());
        consumer.Received(1).Commit(consumeResult);
    }

    [Fact]
    public void ProcessMessage_RetryExhaustion_ShouldPublishToDlqAndCommitOffset()
    {
        // Arrange
        var service = CreateService();
        var eventId = Guid.NewGuid();
        var rawJson = JsonSerializer.Serialize(new
        {
            eventId = eventId,
            eventType = nameof(ProductCreatedEvent),
            productId = Guid.NewGuid(),
            sku = "SKU-DLQ-01",
            name = "DLQ Test",
            unitPrice = 30.0,
            isActive = true
        });

        var consumeResult = CreateConsumeResult("supplychainx.product.events", rawJson);
        var consumer = Substitute.For<IConsumer<string, string>>();

        _idempotencyService.HasBeenProcessedAsync(eventId, Arg.Any<CancellationToken>()).Returns(false);
        _productCreatedHandler.HandleAsync(Arg.Any<ProductCreatedEvent>(), Arg.Any<CancellationToken>())
            .Returns<Task>(x => throw new InvalidOperationException("Permanent logic failure"));

        _eventPublisher.PublishRawAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IDictionary<string, string>>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        service.ProcessMessage(consumer, consumeResult, CancellationToken.None);

        // Assert
        _eventPublisher.Received(1).PublishRawAsync(
            "supplychainx.product.events.dlq",
            "test-key",
            rawJson,
            Arg.Is<IDictionary<string, string>>(h =>
                h["x-original-topic"] == "supplychainx.product.events" &&
                h["x-event-id"] == eventId.ToString() &&
                h["x-exception-message"] == "Permanent logic failure"),
            Arg.Any<CancellationToken>());

        consumer.Received(1).Commit(consumeResult);
        _idempotencyService.DidNotReceiveWithAnyArgs().MarkAsProcessedAsync(default, default!, default);
    }

    [Fact]
    public void ProcessMessage_DlqPublicationFailure_ShouldNotCommitOffset()
    {
        // Arrange
        var service = CreateService();
        var eventId = Guid.NewGuid();
        var rawJson = JsonSerializer.Serialize(new
        {
            eventId = eventId,
            eventType = nameof(ProductCreatedEvent),
            productId = Guid.NewGuid(),
            sku = "SKU-DLQ-FAIL",
            name = "DLQ Fail Test",
            unitPrice = 40.0,
            isActive = true
        });

        var consumeResult = CreateConsumeResult("supplychainx.product.events", rawJson);
        var consumer = Substitute.For<IConsumer<string, string>>();

        _idempotencyService.HasBeenProcessedAsync(eventId, Arg.Any<CancellationToken>()).Returns(false);
        _productCreatedHandler.HandleAsync(Arg.Any<ProductCreatedEvent>(), Arg.Any<CancellationToken>())
            .Returns<Task>(x => throw new InvalidOperationException("Permanent failure"));

        _eventPublisher.PublishRawAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IDictionary<string, string>>(), Arg.Any<CancellationToken>())
            .Returns<Task>(x => throw new Exception("Kafka broker offline"));

        // Act
        service.ProcessMessage(consumer, consumeResult, CancellationToken.None);

        // Assert
        consumer.DidNotReceive().Commit(Arg.Any<ConsumeResult<string, string>>());
    }

    [Fact]
    public void ProcessMessage_MalformedJson_ShouldNotCrashConsumerAndShouldCommitOffset()
    {
        // Arrange
        var service = CreateService();
        var rawJson = "{ invalid json payload ";
        var consumeResult = CreateConsumeResult("supplychainx.product.events", rawJson);
        var consumer = Substitute.For<IConsumer<string, string>>();

        // Act
        var act = () => service.ProcessMessage(consumer, consumeResult, CancellationToken.None);

        // Assert
        act.Should().NotThrow();
        consumer.Received(1).Commit(consumeResult);
        _productCreatedHandler.DidNotReceiveWithAnyArgs().HandleAsync(default!, default);
    }

    [Fact]
    public void ProcessMessage_DuplicateEvent_ShouldSkipHandlerAndCommitOffset()
    {
        // Arrange
        var service = CreateService();
        var eventId = Guid.NewGuid();
        var rawJson = JsonSerializer.Serialize(new
        {
            eventId = eventId,
            eventType = nameof(ProductCreatedEvent),
            productId = Guid.NewGuid(),
            sku = "SKU-DUP",
            name = "Duplicate Test",
            unitPrice = 50.0,
            isActive = true
        });

        var consumeResult = CreateConsumeResult("supplychainx.product.events", rawJson);
        var consumer = Substitute.For<IConsumer<string, string>>();

        _idempotencyService.HasBeenProcessedAsync(eventId, Arg.Any<CancellationToken>()).Returns(true);

        // Act
        service.ProcessMessage(consumer, consumeResult, CancellationToken.None);

        // Assert
        _productCreatedHandler.DidNotReceiveWithAnyArgs().HandleAsync(default!, default);
        consumer.Received(1).Commit(consumeResult);
    }

    [Fact]
    public void ProcessMessage_SuccessfulEvent_ShouldCommitOnlyAfterProcessing()
    {
        // Arrange
        var service = CreateService();
        var eventId = Guid.NewGuid();
        var rawJson = JsonSerializer.Serialize(new
        {
            eventId = eventId,
            eventType = nameof(ProductCreatedEvent),
            productId = Guid.NewGuid(),
            sku = "SKU-SUCCESS",
            name = "Success Test",
            unitPrice = 60.0,
            isActive = true
        });

        var consumeResult = CreateConsumeResult("supplychainx.product.events", rawJson);
        var consumer = Substitute.For<IConsumer<string, string>>();

        _idempotencyService.HasBeenProcessedAsync(eventId, Arg.Any<CancellationToken>()).Returns(false);

        // Act
        service.ProcessMessage(consumer, consumeResult, CancellationToken.None);

        // Assert
        Received.InOrder(() =>
        {
            _productCreatedHandler.HandleAsync(Arg.Any<ProductCreatedEvent>(), Arg.Any<CancellationToken>());
            _idempotencyService.MarkAsProcessedAsync(eventId, nameof(ProductCreatedEvent), Arg.Any<CancellationToken>());
            consumer.Commit(consumeResult);
        });
    }

    [Fact]
    public void ProcessMessage_FailedEvent_ShouldNotCommitBeforeRetryOrDlq()
    {
        // Arrange
        var service = CreateService();
        var eventId = Guid.NewGuid();
        var rawJson = JsonSerializer.Serialize(new
        {
            eventId = eventId,
            eventType = nameof(ProductCreatedEvent),
            productId = Guid.NewGuid(),
            sku = "SKU-FAILED-NOT-COMMITTED",
            name = "Failed Commit Test",
            unitPrice = 70.0,
            isActive = true
        });

        var consumeResult = CreateConsumeResult("supplychainx.product.events", rawJson);
        var consumer = Substitute.For<IConsumer<string, string>>();

        _idempotencyService.HasBeenProcessedAsync(eventId, Arg.Any<CancellationToken>()).Returns(false);

        _productCreatedHandler.HandleAsync(Arg.Any<ProductCreatedEvent>(), Arg.Any<CancellationToken>())
            .Returns<Task>(x => throw new InvalidOperationException("Fail"));

        _eventPublisher.PublishRawAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IDictionary<string, string>>(), Arg.Any<CancellationToken>())
            .Returns<Task>(x => throw new Exception("DLQ Fail"));

        // Act
        service.ProcessMessage(consumer, consumeResult, CancellationToken.None);

        // Assert
        consumer.DidNotReceive().Commit(Arg.Any<ConsumeResult<string, string>>());
    }

    [Fact]
    public void ProcessMessage_SubsequentValidEvents_ShouldProcessAfterFailedEvent()
    {
        // Arrange
        var service = CreateService();
        var failedEventId = Guid.NewGuid();
        var failedJson = JsonSerializer.Serialize(new
        {
            eventId = failedEventId,
            eventType = nameof(ProductCreatedEvent),
            productId = Guid.NewGuid(),
            sku = "SKU-FAIL-SEQ",
            name = "Failed Event",
            unitPrice = 80.0,
            isActive = true
        });

        var validEventId = Guid.NewGuid();
        var validJson = JsonSerializer.Serialize(new
        {
            eventId = validEventId,
            eventType = nameof(ProductCreatedEvent),
            productId = Guid.NewGuid(),
            sku = "SKU-VALID-SEQ",
            name = "Valid Event",
            unitPrice = 90.0,
            isActive = true
        });

        var consumer = Substitute.For<IConsumer<string, string>>();
        var failedResult = CreateConsumeResult("supplychainx.product.events", failedJson, offset: 1);
        var validResult = CreateConsumeResult("supplychainx.product.events", validJson, offset: 2);

        _idempotencyService.HasBeenProcessedAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(false);

        _productCreatedHandler.HandleAsync(Arg.Is<ProductCreatedEvent>(p => p.EventId == failedEventId), Arg.Any<CancellationToken>())
            .Returns<Task>(x => throw new InvalidOperationException("Permanent failure"));

        _productCreatedHandler.HandleAsync(Arg.Is<ProductCreatedEvent>(p => p.EventId == validEventId), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        _eventPublisher.PublishRawAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IDictionary<string, string>>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act 1: Process failed message (goes to DLQ)
        service.ProcessMessage(consumer, failedResult, CancellationToken.None);

        // Act 2: Process subsequent valid message
        service.ProcessMessage(consumer, validResult, CancellationToken.None);

        // Assert
        consumer.Received(1).Commit(failedResult);
        consumer.Received(1).Commit(validResult);
        _idempotencyService.Received(1).MarkAsProcessedAsync(validEventId, nameof(ProductCreatedEvent), Arg.Any<CancellationToken>());
    }
}
