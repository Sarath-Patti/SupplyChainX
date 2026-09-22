package com.supplychainx.processservice.analytics.dto;

public record ActivityReworkResponse(
    String activityName,
    long totalExecutionCount,
    long reworkOccurrences,
    long affectedProcessCount,
    double averageReworkOccurrencesPerAffectedProcess,
    double reworkContributionPercentage
) {
}
