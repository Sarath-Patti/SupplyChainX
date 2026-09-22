package com.supplychainx.processservice.analytics.dto;

import java.time.Instant;
import java.util.UUID;

public record ProcessMetricsResponse(
    UUID processInstanceId,
    String businessKey,
    String processType,
    String status,
    Long cycleTimeMs,
    Long elapsedDurationMs,
    int stageCount,
    int eventCount,
    Instant startedAt,
    Instant completedAt
) {
}
