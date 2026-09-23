package com.supplychainx.processservice.metrics;

import io.micrometer.core.instrument.Counter;
import io.micrometer.core.instrument.MeterRegistry;
import io.micrometer.core.instrument.Timer;
import org.springframework.stereotype.Component;

import java.util.concurrent.TimeUnit;

@Component
public class ProcessAnalyticsMetrics {

    private final MeterRegistry registry;

    public ProcessAnalyticsMetrics(MeterRegistry registry) {
        this.registry = registry;
    }

    public void incrementAnalyticsRequest(String endpoint, String method, int status) {
        Counter.builder("analytics.requests.total")
            .tag("endpoint", endpoint)
            .tag("method", method)
            .tag("status", String.valueOf(status))
            .description("Total number of process analytics API requests")
            .register(registry)
            .increment();
    }

    public void incrementConformanceRequest(String status) {
        Counter.builder("analytics.conformance.requests.total")
            .tag("status", status)
            .description("Total process conformance requests")
            .register(registry)
            .increment();
    }

    public void incrementVariantsRequest(String status) {
        Counter.builder("analytics.variants.requests.total")
            .tag("status", status)
            .description("Total process variant requests")
            .register(registry)
            .increment();
    }

    public void incrementReworkRequest(String status) {
        Counter.builder("analytics.rework.requests.total")
            .tag("status", status)
            .description("Total process rework requests")
            .register(registry)
            .increment();
    }

    public void incrementEventProcessed(String topic, String eventType, String status) {
        Counter.builder("process.event.processed.total")
            .tag("topic", topic != null ? topic : "unknown")
            .tag("eventType", eventType != null ? eventType : "unknown")
            .tag("status", status)
            .description("Total processed domain events")
            .register(registry)
            .increment();
    }

    public void incrementEventFailure(String eventType, String errorType) {
        Counter.builder("process.event.failures.total")
            .tag("eventType", eventType != null ? eventType : "unknown")
            .tag("errorType", errorType != null ? errorType : "unknown")
            .description("Total failed domain event processing attempts")
            .register(registry)
            .increment();
    }

    public void recordKafkaConsumerProcessing(String topic, String status, long durationMs) {
        Counter.builder("kafka.consumer.activity.total")
            .tag("topic", topic != null ? topic : "unknown")
            .tag("status", status)
            .description("Kafka consumer activity counter")
            .register(registry)
            .increment();

        Timer.builder("kafka.consumer.processing.time")
            .tag("topic", topic != null ? topic : "unknown")
            .tag("status", status)
            .description("Kafka consumer event processing duration")
            .register(registry)
            .record(durationMs, TimeUnit.MILLISECONDS);
    }
}
