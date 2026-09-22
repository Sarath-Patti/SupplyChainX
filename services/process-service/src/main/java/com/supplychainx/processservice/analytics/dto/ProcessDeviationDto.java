package com.supplychainx.processservice.analytics.dto;

public record ProcessDeviationDto(
    String deviationType,
    String activityName,
    Integer expectedPosition,
    Integer actualPosition,
    String description
) {
}
