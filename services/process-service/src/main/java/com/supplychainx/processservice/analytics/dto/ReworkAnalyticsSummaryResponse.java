package com.supplychainx.processservice.analytics.dto;

import java.time.Instant;
import java.util.List;

public record ReworkAnalyticsSummaryResponse(
    String processType,
    long totalCompletedProcesses,
    long reworkedProcessCount,
    long nonReworkedProcessCount,
    double reworkRate,
    long totalReworkOccurrences,
    double averageReworkOccurrencesPerReworkedProcess,
    Double averageCycleTimeWithReworkMs,
    Double averageCycleTimeWithoutReworkMs,
    Long minCycleTimeWithReworkMs,
    Long maxCycleTimeWithReworkMs,
    Long minCycleTimeWithoutReworkMs,
    Long maxCycleTimeWithoutReworkMs,
    Double cycleTimeDifferenceMs,
    List<ActivityReworkResponse> activities,
    Instant from,
    Instant to
) {
}
