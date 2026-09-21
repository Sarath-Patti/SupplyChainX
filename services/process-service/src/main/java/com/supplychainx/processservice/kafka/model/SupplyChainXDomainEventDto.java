package com.supplychainx.processservice.kafka.model;

import com.fasterxml.jackson.annotation.JsonIgnoreProperties;
import com.fasterxml.jackson.annotation.JsonProperty;

import java.time.Instant;
import java.util.UUID;

@JsonIgnoreProperties(ignoreUnknown = true)
public record SupplyChainXDomainEventDto(
    @JsonProperty("eventId") UUID eventId,
    @JsonProperty("occurredOnUtc") Instant occurredOnUtc,
    @JsonProperty("eventType") String eventType,
    @JsonProperty("eventVersion") Object eventVersion,

    // Product event fields
    @JsonProperty("productId") UUID productId,
    @JsonProperty("name") String name,
    @JsonProperty("sku") String sku,

    // Warehouse event fields
    @JsonProperty("warehouseId") UUID warehouseId,
    @JsonProperty("code") String code,

    // Inventory event fields
    @JsonProperty("inventoryId") UUID inventoryId,
    @JsonProperty("quantity") Integer quantity,
    @JsonProperty("reason") String reason
) {
}
