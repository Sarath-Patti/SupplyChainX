package com.supplychainx.processservice.kafka;

import com.fasterxml.jackson.databind.ObjectMapper;
import com.fasterxml.jackson.datatype.jsr310.JavaTimeModule;
import com.supplychainx.processservice.dto.ProcessEventResponseDto;
import com.supplychainx.processservice.kafka.model.SupplyChainXDomainEventDto;
import com.supplychainx.processservice.service.ProcessService;
import org.apache.kafka.clients.consumer.ConsumerRecord;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;
import org.springframework.kafka.support.Acknowledgment;

import java.time.Instant;
import java.util.UUID;

import static org.junit.jupiter.api.Assertions.assertThrows;
import static org.mockito.ArgumentMatchers.any;
import static org.mockito.ArgumentMatchers.eq;
import static org.mockito.Mockito.*;

@ExtendWith(MockitoExtension.class)
class ProcessEventConsumerTests {

    @Mock
    private ProcessService processService;

    @Mock
    private com.supplychainx.processservice.metrics.ProcessAnalyticsMetrics metrics;

    @Mock
    private Acknowledgment ack;

    private ProcessEventConsumer consumer;
    private ObjectMapper objectMapper;

    @BeforeEach
    void setUp() {
        consumer = new ProcessEventConsumer(processService, metrics);
        objectMapper = new ObjectMapper();
        objectMapper.registerModule(new JavaTimeModule());
    }

    @Test
    void shouldConsumeAndProcessValidEventAndAcknowledge() throws Exception {
        UUID eventId = UUID.randomUUID();
        UUID productId = UUID.randomUUID();
        SupplyChainXDomainEventDto dto = new SupplyChainXDomainEventDto(
            eventId, Instant.now(), "ProductCreatedEvent", 1,
            productId, "Widget A", "SKU-001",
            null, null, null, null, null
        );
        String payload = objectMapper.writeValueAsString(dto);

        ConsumerRecord<String, String> record = new ConsumerRecord<>(
            "supplychainx.product.events", 0, 10L, "key-1", payload
        );

        ProcessEventResponseDto responseDto = new ProcessEventResponseDto(
            UUID.randomUUID(), eventId, UUID.randomUUID(), "ProductCreatedEvent", Instant.now(), payload, Instant.now()
        );
        when(processService.processDomainEvent(any(SupplyChainXDomainEventDto.class), eq(payload))).thenReturn(responseDto);

        consumer.consume(record, ack);

        verify(processService, times(1)).processDomainEvent(any(SupplyChainXDomainEventDto.class), eq(payload));
        verify(ack, times(1)).acknowledge();
    }

    @Test
    void shouldAcknowledgeAndSkipEmptyPayload() {
        ConsumerRecord<String, String> record = new ConsumerRecord<>(
            "supplychainx.product.events", 0, 11L, "key-1", "   "
        );

        consumer.consume(record, ack);

        verifyNoInteractions(processService);
        verify(ack, times(1)).acknowledge();
    }

    @Test
    void shouldAcknowledgeAndSkipMalformedJson() {
        ConsumerRecord<String, String> record = new ConsumerRecord<>(
            "supplychainx.product.events", 0, 12L, "key-1", "{invalid-json"
        );

        consumer.consume(record, ack);

        verifyNoInteractions(processService);
        verify(ack, times(1)).acknowledge();
    }

    @Test
    void shouldThrowExceptionWhenProcessingFails() throws Exception {
        UUID eventId = UUID.randomUUID();
        UUID productId = UUID.randomUUID();
        SupplyChainXDomainEventDto dto = new SupplyChainXDomainEventDto(
            eventId, Instant.now(), "ProductCreatedEvent", 1,
            productId, "Widget A", "SKU-001",
            null, null, null, null, null
        );
        String payload = objectMapper.writeValueAsString(dto);

        ConsumerRecord<String, String> record = new ConsumerRecord<>(
            "supplychainx.product.events", 0, 13L, "key-1", payload
        );

        doThrow(new RuntimeException("Database down")).when(processService).processDomainEvent(any(SupplyChainXDomainEventDto.class), eq(payload));

        assertThrows(RuntimeException.class, () -> consumer.consume(record, ack));
        verify(ack, never()).acknowledge();
    }
}
