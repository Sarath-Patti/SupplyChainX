package com.supplychainx.processservice.analytics.dto;

import java.util.List;
import java.util.UUID;

public record ProcessConformanceResponse(
    UUID processInstanceId,
    String processType,
    List<String> expectedSequence,
    List<String> actualSequence,
    String status,
    double conformanceScore,
    long deviationCount,
    List<ProcessDeviationDto> deviations,
    List<String> missingActivities,
    List<String> unexpectedActivities,
    List<String> orderViolations
) {
}
