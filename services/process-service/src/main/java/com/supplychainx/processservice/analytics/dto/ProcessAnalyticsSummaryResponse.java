package com.supplychainx.processservice.analytics.dto;

import java.time.Instant;

public record ProcessAnalyticsSummaryResponse(
    String processType,
    long totalProcesses,
    long completedProcesses,
    long activeProcesses,
    Double averageCycleTimeMs,
    Long minCycleTimeMs,
    Long maxCycleTimeMs,
    Double throughputPerHour,
    Double throughputPerDay,
    Instant from,
    Instant to
) {
}
