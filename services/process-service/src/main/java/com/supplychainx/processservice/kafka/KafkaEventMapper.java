package com.supplychainx.processservice.kafka;

import com.supplychainx.processservice.kafka.model.SupplyChainXDomainEventDto;
import org.springframework.stereotype.Component;

import java.util.Optional;

@Component
public class KafkaEventMapper {

    public Optional<String> extractBusinessKey(SupplyChainXDomainEventDto dto) {
        if (dto.productId() != null) {
            return Optional.of("PRODUCT-" + dto.productId());
        }
        if (dto.warehouseId() != null) {
            return Optional.of("WAREHOUSE-" + dto.warehouseId());
        }
        if (dto.inventoryId() != null) {
            return Optional.of("INVENTORY-" + dto.inventoryId());
        }
        return Optional.empty();
    }

    public String determineProcessType(SupplyChainXDomainEventDto dto) {
        if (dto.productId() != null) {
            return "PRODUCT_LIFECYCLE";
        }
        if (dto.warehouseId() != null) {
            return "WAREHOUSE_LIFECYCLE";
        }
        if (dto.inventoryId() != null) {
            return "INVENTORY_MANAGEMENT";
        }
        return "GENERIC_PROCESS";
    }

    public boolean isStartEvent(SupplyChainXDomainEventDto dto) {
        if (dto.eventType() == null) {
            return false;
        }
        return switch (dto.eventType()) {
            case "ProductCreatedEvent", "WarehouseCreatedEvent", "InventoryAdjustedEvent" -> true;
            default -> false;
        };
    }

    public boolean isTerminalEvent(SupplyChainXDomainEventDto dto) {
        if (dto.eventType() == null) {
            return false;
        }
        return switch (dto.eventType()) {
            case "ProductDeletedEvent", "WarehouseDeletedEvent" -> true;
            default -> false;
        };
    }

    public Optional<String> determineStepName(SupplyChainXDomainEventDto dto) {
        if (dto.eventType() == null) {
            return Optional.empty();
        }
        return switch (dto.eventType()) {
            case "ProductCreatedEvent" -> Optional.of("PRODUCT_CREATION");
            case "ProductUpdatedEvent" -> Optional.of("PRODUCT_UPDATE");
            case "ProductDeletedEvent" -> Optional.of("PRODUCT_DELETION");
            case "WarehouseCreatedEvent" -> Optional.of("WAREHOUSE_CREATION");
            case "WarehouseUpdatedEvent" -> Optional.of("WAREHOUSE_UPDATE");
            case "WarehouseDeletedEvent" -> Optional.of("WAREHOUSE_DELETION");
            case "InventoryAdjustedEvent" -> Optional.of("INVENTORY_ADJUSTMENT");
            default -> Optional.empty();
        };
    }
}
