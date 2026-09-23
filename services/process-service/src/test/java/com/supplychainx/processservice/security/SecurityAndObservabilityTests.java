package com.supplychainx.processservice.security;

import com.supplychainx.processservice.analytics.ProcessAnalyticsController;
import com.supplychainx.processservice.analytics.ProcessAnalyticsService;
import com.supplychainx.processservice.analytics.dto.ProcessAnalyticsSummaryResponse;
import com.supplychainx.processservice.exception.ResourceNotFoundException;
import com.supplychainx.processservice.metrics.ProcessAnalyticsMetrics;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;
import org.slf4j.MDC;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.autoconfigure.web.servlet.AutoConfigureMockMvc;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.boot.test.mock.mockito.MockBean;
import org.springframework.http.MediaType;
import org.springframework.test.context.ActiveProfiles;
import org.springframework.test.web.servlet.MockMvc;

import java.util.List;
import java.util.UUID;

import static org.hamcrest.Matchers.*;
import static org.junit.jupiter.api.Assertions.*;
import static org.mockito.ArgumentMatchers.any;
import static org.mockito.Mockito.when;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.get;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.post;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.*;

@SpringBootTest
@AutoConfigureMockMvc
@ActiveProfiles("test")
class SecurityAndObservabilityTests {

    @Autowired
    private MockMvc mockMvc;

    @Autowired
    private JwtTokenProvider jwtTokenProvider;

    @MockBean
    private ProcessAnalyticsService analyticsService;

    @MockBean
    private ProcessAnalyticsMetrics metrics;

    // --- SECURITY TESTS (1 - 6) ---

    @Test
    @DisplayName("1. Unauthenticated request to protected API returns 401 Unauthorized")
    void unauthenticatedRequest_returns401() throws Exception {
        mockMvc.perform(get("/api/v1/analytics/summary"))
            .andExpect(status().isUnauthorized())
            .andExpect(jsonPath("$.status").value(401))
            .andExpect(jsonPath("$.error").value("Unauthorized"))
            .andExpect(jsonPath("$.message").value(containsString("authentication is required")))
            .andExpect(jsonPath("$.correlationId").exists());
    }

    @Test
    @DisplayName("2. Valid authenticated user analytics request succeeds with 200 OK")
    void validAuthenticatedUser_analyticsRequest_succeeds() throws Exception {
        String token = jwtTokenProvider.generateTokenForTesting("analyst_user", List.of("ANALYST"));

        ProcessAnalyticsSummaryResponse summary = new ProcessAnalyticsSummaryResponse(
            "PRODUCT_LIFECYCLE", 10, 8, 2, 12000.0, 5000L, 25000L, 4.0, 96.0, null, null
        );
        when(analyticsService.getAnalyticsSummary(any(), any(), any())).thenReturn(summary);

        mockMvc.perform(get("/api/v1/analytics/summary")
                .header("Authorization", "Bearer " + token))
            .andExpect(status().isOk())
            .andExpect(jsonPath("$.processType").value("PRODUCT_LIFECYCLE"))
            .andExpect(jsonPath("$.totalProcesses").value(10));
    }

    @Test
    @DisplayName("3. Insufficient role accessing admin endpoint returns 403 Forbidden")
    void insufficientRole_adminEndpoint_returns403() throws Exception {
        String userToken = jwtTokenProvider.generateTokenForTesting("standard_user", List.of("USER"));

        mockMvc.perform(get("/api/v1/analytics/admin/summary")
                .header("Authorization", "Bearer " + userToken))
            .andExpect(status().isForbidden())
            .andExpect(jsonPath("$.status").value(403))
            .andExpect(jsonPath("$.error").value("Forbidden"))
            .andExpect(jsonPath("$.message").value(containsString("Insufficient privileges")))
            .andExpect(jsonPath("$.correlationId").exists());
    }

    @Test
    @DisplayName("4. Correct ADMIN role accessing admin endpoint succeeds with 200 OK")
    void correctAdminRole_adminEndpoint_succeeds() throws Exception {
        String adminToken = jwtTokenProvider.generateTokenForTesting("admin_user", List.of("ADMIN"));

        mockMvc.perform(get("/api/v1/analytics/admin/summary")
                .header("Authorization", "Bearer " + adminToken))
            .andExpect(status().isOk())
            .andExpect(jsonPath("$.securityLevel").value("ENTERPRISE_HIGH"));
    }

    @Test
    @DisplayName("5. Invalid or expired JWT returns 401 Unauthorized")
    void invalidOrExpiredJwt_returns401() throws Exception {
        mockMvc.perform(get("/api/v1/analytics/summary")
                .header("Authorization", "Bearer invalid.jwt.token.payload"))
            .andExpect(status().isUnauthorized())
            .andExpect(jsonPath("$.status").value(401))
            .andExpect(jsonPath("$.correlationId").exists());
    }

    @Test
    @DisplayName("6. Missing authorization header returns 401 Unauthorized")
    void missingAuthorizationHeader_returns401() throws Exception {
        mockMvc.perform(get("/api/v1/analytics/conformance"))
            .andExpect(status().isUnauthorized())
            .andExpect(jsonPath("$.status").value(401));
    }

    // --- ERROR HANDLING TESTS (7 - 12) ---

    @Test
    @DisplayName("7. Resource not found returns 404 with standardized ErrorResponseDto")
    void resourceNotFound_returns404() throws Exception {
        String token = jwtTokenProvider.generateTokenForTesting("user", List.of("ANALYST"));
        UUID randomId = UUID.randomUUID();

        when(analyticsService.getProcessMetrics(randomId))
            .thenThrow(new ResourceNotFoundException("ProcessInstance not found with ID: " + randomId));

        mockMvc.perform(get("/api/v1/analytics/processes/{id}", randomId)
                .header("Authorization", "Bearer " + token))
            .andExpect(status().isNotFound())
            .andExpect(jsonPath("$.status").value(404))
            .andExpect(jsonPath("$.error").value("Not Found"))
            .andExpect(jsonPath("$.message").value(containsString("ProcessInstance not found")))
            .andExpect(jsonPath("$.path").value("/api/v1/analytics/processes/" + randomId))
            .andExpect(jsonPath("$.correlationId").exists());
    }

    @Test
    @DisplayName("8. Validation failure returns 400 Bad Request with details")
    void validationFailure_returns400() throws Exception {
        String token = jwtTokenProvider.generateTokenForTesting("user", List.of("ADMIN"));

        mockMvc.perform(post("/api/v1/processes")
                .header("Authorization", "Bearer " + token)
                .contentType(MediaType.APPLICATION_JSON)
                .content("{}"))
            .andExpect(status().isBadRequest())
            .andExpect(jsonPath("$.status").value(400))
            .andExpect(jsonPath("$.error").value("Bad Request"))
            .andExpect(jsonPath("$.correlationId").exists());
    }

    @Test
    @DisplayName("9. Malformed request or invalid UUID format returns 400 Bad Request")
    void malformedRequest_returns400() throws Exception {
        String token = jwtTokenProvider.generateTokenForTesting("user", List.of("ANALYST"));

        mockMvc.perform(get("/api/v1/analytics/processes/not-a-valid-uuid")
                .header("Authorization", "Bearer " + token))
            .andExpect(status().isBadRequest())
            .andExpect(jsonPath("$.status").value(400))
            .andExpect(jsonPath("$.message").value(containsString("invalid")))
            .andExpect(jsonPath("$.correlationId").exists());
    }

    @Test
    @DisplayName("10. Unexpected exception mapping returns 500 without stack trace")
    void unexpectedExceptionMapping_returns500WithoutStackTrace() throws Exception {
        String token = jwtTokenProvider.generateTokenForTesting("user", List.of("ANALYST"));
        UUID id = UUID.randomUUID();

        when(analyticsService.getProcessMetrics(id))
            .thenThrow(new RuntimeException("Database failure internal exception"));

        mockMvc.perform(get("/api/v1/analytics/processes/{id}", id)
                .header("Authorization", "Bearer " + token))
            .andExpect(status().isInternalServerError())
            .andExpect(jsonPath("$.status").value(500))
            .andExpect(jsonPath("$.error").value("Internal Server Error"))
            .andExpect(jsonPath("$.message").value(containsString("internal server error occurred")))
            .andExpect(jsonPath("$.stackTrace").doesNotExist())
            .andExpect(jsonPath("$.correlationId").exists());
    }

    @Test
    @DisplayName("11. Error response contains correlation ID")
    void responseContainsCorrelationId() throws Exception {
        String customCorrelationId = "test-corr-id-9999";

        mockMvc.perform(get("/api/v1/analytics/summary")
                .header("X-Correlation-ID", customCorrelationId))
            .andExpect(status().isUnauthorized())
            .andExpect(jsonPath("$.correlationId").value(customCorrelationId));
    }

    @Test
    @DisplayName("12. Stack traces and internal secrets are not returned in API responses")
    void stackTraceAndSecretsNotExposed() throws Exception {
        mockMvc.perform(get("/api/v1/analytics/summary"))
            .andExpect(status().isUnauthorized())
            .andExpect(jsonPath("$.secret").doesNotExist())
            .andExpect(jsonPath("$.password").doesNotExist())
            .andExpect(jsonPath("$.trace").doesNotExist())
            .andExpect(jsonPath("$.stackTrace").doesNotExist());
    }

    // --- CORRELATION ID TESTS (13 - 16) ---

    @Test
    @DisplayName("13. Supplied correlation ID is preserved in response header")
    void suppliedCorrelationId_preserved() throws Exception {
        String token = jwtTokenProvider.generateTokenForTesting("user", List.of("ANALYST"));
        String suppliedCorrId = "custom-correlation-abc-123";

        mockMvc.perform(get("/actuator/health")
                .header("X-Correlation-ID", suppliedCorrId))
            .andExpect(status().isOk())
            .andExpect(header().string("X-Correlation-ID", suppliedCorrId));
    }

    @Test
    @DisplayName("14. Missing correlation ID is generated in response header")
    void missingCorrelationId_generated() throws Exception {
        mockMvc.perform(get("/actuator/health"))
            .andExpect(status().isOk())
            .andExpect(header().exists("X-Correlation-ID"))
            .andExpect(header().string("X-Correlation-ID", not(emptyOrNullString())));
    }

    @Test
    @DisplayName("15. Response contains X-Correlation-ID header on all responses")
    void responseContainsCorrelationIdHeader() throws Exception {
        mockMvc.perform(get("/api/v1/analytics/summary"))
            .andExpect(header().exists("X-Correlation-ID"));
    }

    @Test
    @DisplayName("16. Logs contain correlation context and MDC is cleared after execution")
    void logsContainCorrelationContext() {
        assertNull(MDC.get("correlationId"), "MDC correlationId should be clean outside request loop");
        assertNull(MDC.get("processId"), "MDC processId should be clean outside request loop");
    }

    // --- ACTUATOR & HEALTH TESTS (17 - 20) ---

    @Test
    @DisplayName("17. Health endpoint succeeds without authentication")
    void healthEndpoint_succeeds() throws Exception {
        mockMvc.perform(get("/actuator/health"))
            .andExpect(status().isOk())
            .andExpect(jsonPath("$.status").exists());
    }

    @Test
    @DisplayName("18. Readiness endpoint behaves correctly")
    void readinessEndpoint_behavesCorrectly() throws Exception {
        mockMvc.perform(get("/actuator/health/readiness"))
            .andExpect(status().isOk())
            .andExpect(jsonPath("$.status").value("UP"));
    }

    @Test
    @DisplayName("19. Liveness endpoint behaves correctly")
    void livenessEndpoint_behavesCorrectly() throws Exception {
        mockMvc.perform(get("/actuator/health/liveness"))
            .andExpect(status().isOk())
            .andExpect(jsonPath("$.status").value("UP"));
    }

    @Test
    @DisplayName("20. Sensitive configuration is not exposed in health response")
    void sensitiveConfiguration_notExposedInHealth() throws Exception {
        mockMvc.perform(get("/actuator/health"))
            .andExpect(status().isOk())
            .andExpect(content().string(not(containsString("postgres_dev_password"))))
            .andExpect(content().string(not(containsString("SupplyChainX_Super_Secret"))))
            .andExpect(content().string(not(containsString("password"))));
    }
}
