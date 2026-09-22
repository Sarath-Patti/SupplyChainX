package com.supplychainx.processservice.analytics.dto;

import java.time.Instant;
import java.util.UUID;

public record StageMetricsResponse(
    UUID stageId,
    UUID processInstanceId,
    String stageName,
    String status,
    Instant startedAt,
    Instant completedAt,
    Long durationMs
) {
}
