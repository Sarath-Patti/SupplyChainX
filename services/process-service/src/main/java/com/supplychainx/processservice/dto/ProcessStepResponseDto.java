package com.supplychainx.processservice.dto;

import java.time.Instant;
import java.util.UUID;

public record ProcessStepResponseDto(
    UUID id,
    UUID processInstanceId,
    String stepName,
    String status,
    Instant startedAt,
    Instant completedAt,
    Instant createdAt
) {}
