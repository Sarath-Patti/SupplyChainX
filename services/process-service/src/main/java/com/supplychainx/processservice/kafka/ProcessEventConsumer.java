package com.supplychainx.processservice.kafka;

import com.fasterxml.jackson.databind.ObjectMapper;
import com.fasterxml.jackson.datatype.jsr310.JavaTimeModule;
import com.supplychainx.processservice.kafka.model.SupplyChainXDomainEventDto;
import com.supplychainx.processservice.service.ProcessService;
import org.apache.kafka.clients.consumer.ConsumerRecord;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.kafka.annotation.KafkaListener;
import org.springframework.kafka.support.Acknowledgment;
import org.springframework.stereotype.Component;

@Component
public class ProcessEventConsumer {

    private static final Logger log = LoggerFactory.getLogger(ProcessEventConsumer.class);

    private final ProcessService processService;
    private final ObjectMapper objectMapper;

    public ProcessEventConsumer(ProcessService processService) {
        this.processService = processService;
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
        String topic = record.topic();
        int partition = record.partition();
        long offset = record.offset();
        String payload = record.value();

        log.debug("Received Kafka message from topic '{}', partition {}, offset {}", topic, partition, offset);

        if (payload == null || payload.isBlank()) {
            log.warn("Received empty payload from topic '{}', partition {}, offset {}. Skipping.", topic, partition, offset);
            ack.acknowledge();
            return;
        }

        try {
            SupplyChainXDomainEventDto eventDto = objectMapper.readValue(payload, SupplyChainXDomainEventDto.class);

            if (eventDto == null || eventDto.eventId() == null) {
                log.error("Failed to parse event or missing eventId from topic '{}', partition {}, offset {}. Payload: {}",
                    topic, partition, offset, payload);
                ack.acknowledge();
                return;
            }

            log.info("Processing eventId {} of type '{}' from topic '{}' (partition {}, offset {})",
                eventDto.eventId(), eventDto.eventType(), topic, partition, offset);

            processService.processDomainEvent(eventDto, payload);
            ack.acknowledge();

        } catch (com.fasterxml.jackson.core.JsonProcessingException e) {
            log.error("Deserialization error for record from topic '{}', partition {}, offset {}: {}",
                topic, partition, offset, e.getMessage());
            ack.acknowledge();
        } catch (IllegalArgumentException e) {
            log.error("Validation error for record from topic '{}', partition {}, offset {}: {}",
                topic, partition, offset, e.getMessage());
            ack.acknowledge();
        } catch (Exception e) {
            log.error("Failed to process Kafka record from topic '{}', partition {}, offset {}: {}",
                topic, partition, offset, e.getMessage(), e);
            throw new RuntimeException("Kafka event processing failed for topic " + topic + " at offset " + offset, e);
        }
    }
}
