package com.supplychainx.processservice.service;

import com.supplychainx.processservice.dto.ProcessEventResponseDto;
import com.supplychainx.processservice.entity.ProcessEvent;
import com.supplychainx.processservice.entity.ProcessInstance;
import com.supplychainx.processservice.kafka.KafkaEventMapper;
import com.supplychainx.processservice.kafka.model.SupplyChainXDomainEventDto;
import com.supplychainx.processservice.repository.ProcessEventRepository;
import com.supplychainx.processservice.repository.ProcessInstanceRepository;
import com.supplychainx.processservice.repository.ProcessStepRepository;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.autoconfigure.orm.jpa.DataJpaTest;
import org.springframework.test.context.ActiveProfiles;

import java.time.Instant;
import java.util.List;
import java.util.UUID;

import static org.junit.jupiter.api.Assertions.*;

@DataJpaTest
@ActiveProfiles("test")
class ProcessServiceTests {

    @Autowired
    private ProcessInstanceRepository instanceRepository;

    @Autowired
    private ProcessEventRepository eventRepository;

    @Autowired
    private ProcessStepRepository stepRepository;

    private ProcessService processService;

    @BeforeEach
    void setUp() {
        KafkaEventMapper mapper = new KafkaEventMapper();
        processService = new ProcessService(instanceRepository, eventRepository, stepRepository, mapper);
    }

    @Test
    void shouldCreateInstanceAndPersistEventOnFirstDomainEvent() {
        UUID eventId = UUID.randomUUID();
        UUID productId = UUID.randomUUID();
        Instant now = Instant.now();

        SupplyChainXDomainEventDto dto = new SupplyChainXDomainEventDto(
            eventId, now, "ProductCreatedEvent", 1,
            productId, "Widget Delta", "SKU-DLT-100",
            null, null, null, null, null
        );

        ProcessEventResponseDto response = processService.processDomainEvent(dto, "{\"sku\":\"SKU-DLT-100\"}");

        assertNotNull(response);
        assertEquals(eventId, response.eventId());
        assertEquals("ProductCreatedEvent", response.eventType());

        ProcessInstance instance = instanceRepository.findByBusinessKey("PRODUCT-" + productId).orElseThrow();
        assertEquals("PRODUCT_LIFECYCLE", instance.getProcessType());
        assertEquals("ACTIVE", instance.getStatus());
        assertEquals(1, instance.getEvents().size());
        assertEquals(1, instance.getSteps().size());
        assertEquals("PRODUCT_CREATION", instance.getSteps().get(0).getStepName());
    }

    @Test
    void shouldAttachSubsequentEventToExistingInstance() {
        UUID productId = UUID.randomUUID();
        UUID eventId1 = UUID.randomUUID();
        UUID eventId2 = UUID.randomUUID();
        Instant t1 = Instant.now().minusSeconds(60);
        Instant t2 = Instant.now();

        SupplyChainXDomainEventDto dto1 = new SupplyChainXDomainEventDto(
            eventId1, t1, "ProductCreatedEvent", 1,
            productId, "Widget Epsilon", "SKU-EPS",
            null, null, null, null, null
        );

        SupplyChainXDomainEventDto dto2 = new SupplyChainXDomainEventDto(
            eventId2, t2, "ProductUpdatedEvent", 1,
            productId, "Widget Epsilon Updated", "SKU-EPS",
            null, null, null, null, null
        );

        processService.processDomainEvent(dto1, "{\"version\":1}");
        processService.processDomainEvent(dto2, "{\"version\":2}");

        ProcessInstance instance = instanceRepository.findByBusinessKey("PRODUCT-" + productId).orElseThrow();
        assertEquals(2, eventRepository.findByProcessInstanceIdOrderByTimestampAsc(instance.getId()).size());
        assertEquals(2, instance.getSteps().size());
    }

    @Test
    void shouldIgnoreDuplicateEventWithSameEventId() {
        UUID productId = UUID.randomUUID();
        UUID eventId = UUID.randomUUID();
        Instant now = Instant.now();

        SupplyChainXDomainEventDto dto = new SupplyChainXDomainEventDto(
            eventId, now, "ProductCreatedEvent", 1,
            productId, "Widget Zeta", "SKU-ZET",
            null, null, null, null, null
        );

        processService.processDomainEvent(dto, "{\"version\":1}");
        processService.processDomainEvent(dto, "{\"version\":1}"); // duplicate call

        ProcessInstance instance = instanceRepository.findByBusinessKey("PRODUCT-" + productId).orElseThrow();
        assertEquals(1, eventRepository.findByProcessInstanceIdOrderByTimestampAsc(instance.getId()).size());
    }

    @Test
    void shouldMarkInstanceCompletedOnTerminalEvent() {
        UUID productId = UUID.randomUUID();
        UUID createEventId = UUID.randomUUID();
        UUID deleteEventId = UUID.randomUUID();
        Instant t1 = Instant.now().minusSeconds(100);
        Instant t2 = Instant.now();

        SupplyChainXDomainEventDto createDto = new SupplyChainXDomainEventDto(
            createEventId, t1, "ProductCreatedEvent", 1,
            productId, "Temp Product", "SKU-TMP",
            null, null, null, null, null
        );

        SupplyChainXDomainEventDto deleteDto = new SupplyChainXDomainEventDto(
            deleteEventId, t2, "ProductDeletedEvent", 1,
            productId, null, null,
            null, null, null, null, null
        );

        processService.processDomainEvent(createDto, "{}");
        processService.processDomainEvent(deleteDto, "{}");

        ProcessInstance instance = instanceRepository.findByBusinessKey("PRODUCT-" + productId).orElseThrow();
        assertEquals("COMPLETED", instance.getStatus());
        assertEquals(t2, instance.getCompletedAt());
    }

    @Test
    void shouldMaintainCorrectTimestampOrderingForEvents() {
        UUID productId = UUID.randomUUID();
        UUID eventIdLate = UUID.randomUUID();
        UUID eventIdEarly = UUID.randomUUID();
        Instant earlyTime = Instant.now().minusSeconds(300);
        Instant lateTime = Instant.now();

        SupplyChainXDomainEventDto lateDto = new SupplyChainXDomainEventDto(
            eventIdLate, lateTime, "ProductUpdatedEvent", 1,
            productId, "Product", "SKU-1",
            null, null, null, null, null
        );

        SupplyChainXDomainEventDto earlyDto = new SupplyChainXDomainEventDto(
            eventIdEarly, earlyTime, "ProductCreatedEvent", 1,
            productId, "Product", "SKU-1",
            null, null, null, null, null
        );

        // Process late event first, then early event
        processService.processDomainEvent(lateDto, " late ");
        processService.processDomainEvent(earlyDto, " early ");

        ProcessInstance instance = instanceRepository.findByBusinessKey("PRODUCT-" + productId).orElseThrow();
        List<ProcessEvent> events = eventRepository.findByProcessInstanceIdOrderByTimestampAsc(instance.getId());

        assertEquals(2, events.size());
        assertEquals(eventIdEarly, events.get(0).getEventId());
        assertEquals(eventIdLate, events.get(1).getEventId());
    }
}
