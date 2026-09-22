package com.supplychainx.processservice.analytics.dto;

import java.time.Instant;
import java.util.Map;
import java.util.UUID;

public record ProcessReworkDetailResponse(
    UUID processInstanceId,
    String businessKey,
    String processType,
    String status,
    String variantKey,
    boolean hasRework,
    long totalReworkOccurrences,
    Map<String, Long> repeatedActivities,
    Long cycleTimeMs,
    Instant completedAt
) {
}
