package com.supplychainx.processservice.repository;

import com.supplychainx.processservice.entity.ProcessEvent;
import com.supplychainx.processservice.entity.ProcessInstance;
import com.supplychainx.processservice.entity.ProcessStep;
import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.autoconfigure.orm.jpa.DataJpaTest;
import org.springframework.test.context.ActiveProfiles;

import java.time.Instant;
import java.time.temporal.ChronoUnit;
import java.util.List;
import java.util.Optional;
import java.util.UUID;

import static org.assertj.core.api.Assertions.assertThat;

@DataJpaTest
@ActiveProfiles("test")
class ProcessRepositoryTests {

    @Autowired
    private ProcessInstanceRepository instanceRepository;

    @Autowired
    private ProcessEventRepository eventRepository;

    @Autowired
    private ProcessStepRepository stepRepository;

    @Test
    void shouldPersistAndRetrieveProcessInstanceByBusinessKey() {
        String businessKey = "ORDER-BIZ-1001";
        ProcessInstance instance = new ProcessInstance(
            businessKey,
            "ORDER_FULFILLMENT",
            "RUNNING",
            Instant.now()
        );

        ProcessInstance saved = instanceRepository.save(instance);

        assertThat(saved.getId()).isNotNull();
        assertThat(saved.getCreatedAt()).isNotNull();

        Optional<ProcessInstance> found = instanceRepository.findByBusinessKey(businessKey);
        assertThat(found).isPresent();
        assertThat(found.get().getProcessType()).isEqualTo("ORDER_FULFILLMENT");
        assertThat(instanceRepository.existsByBusinessKey(businessKey)).isTrue();
    }

    @Test
    void shouldPersistProcessEventAndCheckDuplicateEventId() {
        ProcessInstance instance = instanceRepository.save(new ProcessInstance(
            "INV-BIZ-2002",
            "INVENTORY_ADJUSTMENT",
            "COMPLETED",
            Instant.now()
        ));

        UUID eventId = UUID.randomUUID();
        ProcessEvent event = new ProcessEvent(
            eventId,
            "InventoryAdjustedEvent",
            Instant.now(),
            "{\"quantity\": 10}"
        );
        instance.addEvent(event);

        eventRepository.save(event);

        assertThat(eventRepository.existsByEventId(eventId)).isTrue();
        assertThat(eventRepository.existsByEventId(UUID.randomUUID())).isFalse();
    }

    @Test
    void shouldRetrieveProcessEventsOrderedByTimestampAscending() {
        ProcessInstance instance = instanceRepository.save(new ProcessInstance(
            "ORDER-BIZ-3003",
            "ORDER_FULFILLMENT",
            "RUNNING",
            Instant.now()
        ));

        Instant now = Instant.now();
        Instant t1 = now.minus(10, ChronoUnit.MINUTES);
        Instant t2 = now.minus(5, ChronoUnit.MINUTES);
        Instant t3 = now;

        ProcessEvent e2 = new ProcessEvent(UUID.randomUUID(), "OrderAllocatedEvent", t2, "{\"step\":\"2\"}");
        ProcessEvent e1 = new ProcessEvent(UUID.randomUUID(), "OrderCreatedEvent", t1, "{\"step\":\"1\"}");
        ProcessEvent e3 = new ProcessEvent(UUID.randomUUID(), "OrderShippedEvent", t3, "{\"step\":\"3\"}");

        instance.addEvent(e2);
        instance.addEvent(e1);
        instance.addEvent(e3);

        eventRepository.saveAll(List.of(e2, e1, e3));

        List<ProcessEvent> orderedEvents = eventRepository.findByProcessInstanceIdOrderByTimestampAsc(instance.getId());

        assertThat(orderedEvents).hasSize(3);
        assertThat(orderedEvents.get(0).getEventType()).isEqualTo("OrderCreatedEvent");
        assertThat(orderedEvents.get(1).getEventType()).isEqualTo("OrderAllocatedEvent");
        assertThat(orderedEvents.get(2).getEventType()).isEqualTo("OrderShippedEvent");
    }

    @Test
    void shouldPersistProcessSteps() {
        ProcessInstance instance = instanceRepository.save(new ProcessInstance(
            "PROD-BIZ-4004",
            "PRODUCT_ONBOARDING",
            "RUNNING",
            Instant.now()
        ));

        ProcessStep step = new ProcessStep("VALIDATE_SKU", "COMPLETED", Instant.now());
        instance.addStep(step);

        stepRepository.save(step);

        List<ProcessStep> steps = stepRepository.findByProcessInstanceId(instance.getId());
        assertThat(steps).hasSize(1);
        assertThat(steps.get(0).getStepName()).isEqualTo("VALIDATE_SKU");
    }
}
