package com.supplychainx.processservice.kafka;

import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;
import com.fasterxml.jackson.datatype.jsr310.JavaTimeModule;
import com.supplychainx.processservice.kafka.model.SupplyChainXDomainEventDto;
import com.supplychainx.processservice.metrics.ProcessAnalyticsMetrics;
import com.supplychainx.processservice.service.ProcessService;
import org.apache.kafka.clients.consumer.ConsumerRecord;
import org.apache.kafka.common.header.Header;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.slf4j.MDC;
import org.springframework.kafka.annotation.KafkaListener;
import org.springframework.kafka.support.Acknowledgment;
import org.springframework.stereotype.Component;

import java.nio.charset.StandardCharsets;
import java.util.UUID;

@Component
public class ProcessEventConsumer {

    private static final Logger log = LoggerFactory.getLogger(ProcessEventConsumer.class);

    private final ProcessService processService;
    private final ProcessAnalyticsMetrics metrics;
    private final ObjectMapper objectMapper;

    public ProcessEventConsumer(ProcessService processService, ProcessAnalyticsMetrics metrics) {
        this.processService = processService;
        this.metrics = metrics;
        this.objectMapper = new ObjectMapper();
        this.objectMapper.registerModule(new JavaTimeModule());
    }

    @KafkaListener(
        topics = {
            "${process.kafka.topics.product-events:supplychainx.product.events}",
            "${process.kafka.topics.warehouse-events:supplychainx.warehouse.events}",
            "${process.kafka.topics.inventory-events:supplychainx.inventory.events}"
        },
        containerFactory = "kafkaListenerContainerFactory"
    )
    public void consume(ConsumerRecord<String, String> record, Acknowledgment ack) {
        long startTime = System.currentTimeMillis();
        String topic = record.topic();
        int partition = record.partition();
        long offset = record.offset();
        String payload = record.value();

        String correlationId = extractCorrelationId(record, payload);
        MDC.put("correlationId", correlationId);

        log.debug("Received Kafka message from topic '{}', partition {}, offset {}", topic, partition, offset);

        if (payload == null || payload.isBlank()) {
            log.warn("Received empty payload from topic '{}', partition {}, offset {}. Skipping.", topic, partition, offset);
            metrics.recordKafkaConsumerProcessing(topic, "SKIPPED_EMPTY", System.currentTimeMillis() - startTime);
            ack.acknowledge();
            MDC.clear();
            return;
        }

        String eventIdStr = null;
        String eventTypeStr = null;

        try {
            SupplyChainXDomainEventDto eventDto = objectMapper.readValue(payload, SupplyChainXDomainEventDto.class);

            if (eventDto == null || eventDto.eventId() == null) {
                log.error("Failed to parse event or missing eventId from topic '{}', partition {}, offset {}.", topic, partition, offset);
                metrics.incrementEventFailure("UNKNOWN", "MISSING_EVENT_ID");
                metrics.recordKafkaConsumerProcessing(topic, "FAILURE_PARSING", System.currentTimeMillis() - startTime);
                ack.acknowledge();
                return;
            }

            eventIdStr = eventDto.eventId().toString();
            eventTypeStr = eventDto.eventType() != null ? eventDto.eventType() : "UNKNOWN_EVENT";

            MDC.put("eventId", eventIdStr);
            if (eventDto.productId() != null) {
                MDC.put("processId", eventDto.productId().toString());
            } else if (eventDto.warehouseId() != null) {
                MDC.put("processId", eventDto.warehouseId().toString());
            } else if (eventDto.inventoryId() != null) {
                MDC.put("processId", eventDto.inventoryId().toString());
            }

            log.info("Processing eventId {} of type '{}' from topic '{}' (partition {}, offset {})",
                eventIdStr, eventTypeStr, topic, partition, offset);

            processService.processDomainEvent(eventDto, payload);

            long duration = System.currentTimeMillis() - startTime;
            metrics.incrementEventProcessed(topic, eventTypeStr, "SUCCESS");
            metrics.recordKafkaConsumerProcessing(topic, "SUCCESS", duration);
            log.info("Successfully processed eventId {} in {} ms", eventIdStr, duration);

            ack.acknowledge();

        } catch (com.fasterxml.jackson.core.JsonProcessingException e) {
            long duration = System.currentTimeMillis() - startTime;
            log.error("Deserialization error for record from topic '{}', partition {}, offset {}: {}",
                topic, partition, offset, e.getMessage());
            metrics.incrementEventFailure(eventTypeStr, "DESERIALIZATION_ERROR");
            metrics.recordKafkaConsumerProcessing(topic, "DESERIALIZATION_ERROR", duration);
            ack.acknowledge();
        } catch (IllegalArgumentException e) {
            long duration = System.currentTimeMillis() - startTime;
            log.error("Validation error for record from topic '{}', partition {}, offset {}: {}",
                topic, partition, offset, e.getMessage());
            metrics.incrementEventFailure(eventTypeStr, "VALIDATION_ERROR");
            metrics.recordKafkaConsumerProcessing(topic, "VALIDATION_ERROR", duration);
            ack.acknowledge();
        } catch (Exception e) {
            long duration = System.currentTimeMillis() - startTime;
            log.error("Failed to process Kafka record from topic '{}', partition {}, offset {}: {}",
                topic, partition, offset, e.getMessage(), e);
            metrics.incrementEventFailure(eventTypeStr, "PROCESSING_ERROR");
            metrics.recordKafkaConsumerProcessing(topic, "FAILURE", duration);
            throw new RuntimeException("Kafka event processing failed for topic " + topic + " at offset " + offset, e);
        } finally {
            MDC.clear();
        }
    }

    private String extractCorrelationId(ConsumerRecord<String, String> record, String payload) {
        if (record.headers() != null) {
            Header header = record.headers().lastHeader("X-Correlation-ID");
            if (header == null) {
                header = record.headers().lastHeader("x-correlation-id");
            }
            if (header != null && header.value() != null) {
                return new String(header.value(), StandardCharsets.UTF_8);
            }
        }

        if (payload != null && !payload.isBlank()) {
            try {
                JsonNode node = objectMapper.readTree(payload);
                if (node.has("correlationId") && !node.get("correlationId").isNull()) {
                    return node.get("correlationId").asText();
                }
            } catch (Exception ignored) {
            }
        }

        return UUID.randomUUID().toString();
    }
}
