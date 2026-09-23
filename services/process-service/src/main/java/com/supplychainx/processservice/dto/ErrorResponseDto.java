package com.supplychainx.processservice.dto;

import org.slf4j.MDC;
import java.time.Instant;

public record ErrorResponseDto(
    Instant timestamp,
    int status,
    String error,
    String message,
    String path,
    String correlationId
) {
    public ErrorResponseDto(Instant timestamp, int status, String error, String message, String path) {
        this(timestamp, status, error, message, path, MDC.get("correlationId"));
    }
}
