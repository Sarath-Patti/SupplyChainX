package com.supplychainx.processservice.analytics.dto;

public record StageDurationStats(
    String stageName,
    long executionCount,
    double averageDurationMs,
    long minDurationMs,
    long maxDurationMs,
    long totalDurationMs
) {
}
