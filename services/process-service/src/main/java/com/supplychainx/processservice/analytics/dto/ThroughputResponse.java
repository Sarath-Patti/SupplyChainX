package com.supplychainx.processservice.analytics.dto;

import java.time.Instant;

public record ThroughputResponse(
    String processType,
    long completedProcesses,
    double timeWindowHours,
    double timeWindowDays,
    Double throughputPerHour,
    Double throughputPerDay,
    Instant from,
    Instant to
) {
}
