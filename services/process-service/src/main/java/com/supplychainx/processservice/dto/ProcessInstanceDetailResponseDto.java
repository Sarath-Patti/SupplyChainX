package com.supplychainx.processservice.dto;

import java.time.Instant;
import java.util.List;
import java.util.UUID;

public record ProcessInstanceDetailResponseDto(
    UUID id,
    String businessKey,
    String processType,
    String status,
    Instant startedAt,
    Instant completedAt,
    Instant createdAt,
    Instant updatedAt,
    List<ProcessStepResponseDto> steps,
    List<ProcessEventResponseDto> events
) {}
