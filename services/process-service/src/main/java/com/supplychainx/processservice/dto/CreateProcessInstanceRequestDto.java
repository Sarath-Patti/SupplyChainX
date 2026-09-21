package com.supplychainx.processservice.dto;

import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.Size;

import java.time.Instant;

public record CreateProcessInstanceRequestDto(
    @NotBlank(message = "businessKey is required")
    @Size(max = 128, message = "businessKey cannot exceed 128 characters")
    String businessKey,

    @NotBlank(message = "processType is required")
    @Size(max = 64, message = "processType cannot exceed 64 characters")
    String processType,

    @NotBlank(message = "status is required")
    @Size(max = 32, message = "status cannot exceed 32 characters")
    String status,

    Instant startedAt
) {}
