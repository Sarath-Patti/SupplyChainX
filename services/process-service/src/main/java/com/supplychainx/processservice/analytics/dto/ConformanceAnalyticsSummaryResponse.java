package com.supplychainx.processservice.analytics.dto;

import java.time.Instant;
import java.util.Map;

public record ConformanceAnalyticsSummaryResponse(
    String processType,
    long totalProcessesAnalyzed,
    long conformantProcessCount,
    long deviatedProcessCount,
    double conformanceRate,
    double averageConformanceScore,
    long totalDeviationCount,
    Map<String, Long> deviationCountsByType,
    Instant from,
    Instant to
) {
}
