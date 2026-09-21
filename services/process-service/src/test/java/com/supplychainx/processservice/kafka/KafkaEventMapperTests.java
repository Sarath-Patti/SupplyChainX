package com.supplychainx.processservice.kafka;

import com.supplychainx.processservice.kafka.model.SupplyChainXDomainEventDto;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;

import java.time.Instant;
import java.util.Optional;
import java.util.UUID;

import static org.junit.jupiter.api.Assertions.*;

class KafkaEventMapperTests {

    private KafkaEventMapper mapper;

    @BeforeEach
    void setUp() {
        mapper = new KafkaEventMapper();
    }

    @Test
    void shouldMapProductCreatedEventCorrectly() {
        UUID productId = UUID.randomUUID();
        UUID eventId = UUID.randomUUID();
        SupplyChainXDomainEventDto dto = new SupplyChainXDomainEventDto(
            eventId, Instant.now(), "ProductCreatedEvent", 1,
            productId, "Test Product", "SKU-999",
            null, null,
            null, null, null
        );

        Optional<String> businessKey = mapper.extractBusinessKey(dto);
        assertTrue(businessKey.isPresent());
        assertEquals("PRODUCT-" + productId, businessKey.get());

        assertEquals("PRODUCT_LIFECYCLE", mapper.determineProcessType(dto));
        assertTrue(mapper.isStartEvent(dto));
        assertFalse(mapper.isTerminalEvent(dto));
        assertEquals(Optional.of("PRODUCT_CREATION"), mapper.determineStepName(dto));
    }

    @Test
    void shouldMapProductDeletedEventAsTerminal() {
        UUID productId = UUID.randomUUID();
        UUID eventId = UUID.randomUUID();
        SupplyChainXDomainEventDto dto = new SupplyChainXDomainEventDto(
            eventId, Instant.now(), "ProductDeletedEvent", 1,
            productId, null, null,
            null, null,
            null, null, null
        );

        assertTrue(mapper.isTerminalEvent(dto));
        assertEquals(Optional.of("PRODUCT_DELETION"), mapper.determineStepName(dto));
    }

    @Test
    void shouldMapWarehouseEventsCorrectly() {
        UUID warehouseId = UUID.randomUUID();
        UUID eventId = UUID.randomUUID();
        SupplyChainXDomainEventDto dto = new SupplyChainXDomainEventDto(
            eventId, Instant.now(), "WarehouseCreatedEvent", 1,
            null, null, null,
            warehouseId, "WH-001",
            null, null, null
        );

        Optional<String> businessKey = mapper.extractBusinessKey(dto);
        assertTrue(businessKey.isPresent());
        assertEquals("WAREHOUSE-" + warehouseId, businessKey.get());
        assertEquals("WAREHOUSE_LIFECYCLE", mapper.determineProcessType(dto));
        assertEquals(Optional.of("WAREHOUSE_CREATION"), mapper.determineStepName(dto));
    }

    @Test
    void shouldMapInventoryEventsCorrectly() {
        UUID inventoryId = UUID.randomUUID();
        UUID eventId = UUID.randomUUID();
        SupplyChainXDomainEventDto dto = new SupplyChainXDomainEventDto(
            eventId, Instant.now(), "InventoryAdjustedEvent", 1,
            null, null, null,
            null, null,
            inventoryId, 50, "Restock"
        );

        Optional<String> businessKey = mapper.extractBusinessKey(dto);
        assertTrue(businessKey.isPresent());
        assertEquals("INVENTORY-" + inventoryId, businessKey.get());
        assertEquals("INVENTORY_MANAGEMENT", mapper.determineProcessType(dto));
        assertEquals(Optional.of("INVENTORY_ADJUSTMENT"), mapper.determineStepName(dto));
    }

    @Test
    void shouldReturnEmptyWhenNoIdentifierPresent() {
        SupplyChainXDomainEventDto dto = new SupplyChainXDomainEventDto(
            UUID.randomUUID(), Instant.now(), "UnknownEvent", 1,
            null, null, null,
            null, null,
            null, null, null
        );

        assertTrue(mapper.extractBusinessKey(dto).isEmpty());
        assertEquals("GENERIC_PROCESS", mapper.determineProcessType(dto));
        assertTrue(mapper.determineStepName(dto).isEmpty());
    }
}
