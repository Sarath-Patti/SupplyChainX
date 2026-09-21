package com.supplychainx.processservice.dto;

import java.time.Instant;
import java.util.UUID;

public record ProcessEventResponseDto(
    UUID id,
    UUID eventId,
    UUID processInstanceId,
    String eventType,
    Instant timestamp,
    String payload,
    Instant createdAt
) {}
