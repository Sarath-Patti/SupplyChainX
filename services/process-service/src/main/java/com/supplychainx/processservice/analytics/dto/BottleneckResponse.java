package com.supplychainx.processservice.analytics.dto;

public record BottleneckResponse(
    String stageName,
    long executionCount,
    double averageDurationMs,
    long minDurationMs,
    long maxDurationMs,
    long totalDurationMs,
    double processTimeContribution
) {
}
