package com.supplychainx.processservice.dto;

import java.time.Instant;
import java.util.UUID;

public record ProcessInstanceResponseDto(
    UUID id,
    String businessKey,
    String processType,
    String status,
    Instant startedAt,
    Instant completedAt,
    Instant createdAt,
    Instant updatedAt
) {}
