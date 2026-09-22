package com.supplychainx.processservice.analytics.dto;

import java.util.List;

public record ProcessVariantResponse(
    String variantKey,
    String processType,
    List<String> sequence,
    long occurrenceCount,
    double percentage,
    long completedCount,
    Double averageCycleTimeMs,
    Long minCycleTimeMs,
    Long maxCycleTimeMs,
    Long totalCycleTimeMs
) {
}
