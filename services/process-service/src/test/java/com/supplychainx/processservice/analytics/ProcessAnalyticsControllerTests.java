package com.supplychainx.processservice.analytics;

import com.supplychainx.processservice.analytics.dto.*;
import com.supplychainx.processservice.exception.ResourceNotFoundException;
import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.autoconfigure.web.servlet.WebMvcTest;
import org.springframework.boot.test.mock.mockito.MockBean;
import org.springframework.http.MediaType;
import org.springframework.test.context.ActiveProfiles;
import org.springframework.test.web.servlet.MockMvc;

import java.time.Instant;
import java.util.Collections;
import java.util.List;
import java.util.UUID;

import static org.mockito.ArgumentMatchers.any;
import static org.mockito.ArgumentMatchers.eq;
import static org.mockito.Mockito.when;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.get;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.*;

@WebMvcTest(ProcessAnalyticsController.class)
@ActiveProfiles("test")
class ProcessAnalyticsControllerTests {

    @Autowired
    private MockMvc mockMvc;

    @MockBean
    private ProcessAnalyticsService analyticsService;

    @Test
    void shouldReturnProcessMetricsWhenFound() throws Exception {
        UUID processId = UUID.randomUUID();
        Instant now = Instant.now();

        ProcessMetricsResponse dto = new ProcessMetricsResponse(
            processId,
            "PRODUCT-123",
            "PRODUCT_LIFECYCLE",
            "COMPLETED",
            12000L,
            12000L,
            2,
            3,
            now.minusSeconds(12),
            now
        );

        when(analyticsService.getProcessMetrics(processId)).thenReturn(dto);

        mockMvc.perform(get("/api/v1/analytics/processes/{id}", processId))
            .andExpect(status().isOk())
            .andExpect(content().contentType(MediaType.APPLICATION_JSON))
            .andExpect(jsonPath("$.processInstanceId").value(processId.toString()))
            .andExpect(jsonPath("$.businessKey").value("PRODUCT-123"))
            .andExpect(jsonPath("$.cycleTimeMs").value(12000));
    }

    @Test
    void shouldReturn404WhenProcessNotFound() throws Exception {
        UUID processId = UUID.randomUUID();
        when(analyticsService.getProcessMetrics(processId))
            .thenThrow(new ResourceNotFoundException("ProcessInstance not found with ID: " + processId));

        mockMvc.perform(get("/api/v1/analytics/processes/{id}", processId))
            .andExpect(status().isNotFound())
            .andExpect(jsonPath("$.status").value(404));
    }

    @Test
    void shouldReturnStageMetrics() throws Exception {
        UUID processId = UUID.randomUUID();
        UUID stageId = UUID.randomUUID();
        Instant now = Instant.now();

        StageMetricsResponse stage = new StageMetricsResponse(
            stageId,
            processId,
            "PRODUCT_CREATION",
            "COMPLETED",
            now.minusSeconds(10),
            now,
            10000L
        );

        when(analyticsService.getProcessStageMetrics(processId)).thenReturn(List.of(stage));

        mockMvc.perform(get("/api/v1/analytics/processes/{id}/stages", processId))
            .andExpect(status().isOk())
            .andExpect(jsonPath("$[0].stageName").value("PRODUCT_CREATION"))
            .andExpect(jsonPath("$[0].durationMs").value(10000));
    }

    @Test
    void shouldReturnAnalyticsSummary() throws Exception {
        ProcessAnalyticsSummaryResponse summary = new ProcessAnalyticsSummaryResponse(
            "PRODUCT_LIFECYCLE",
            10,
            8,
            2,
            15000.0,
            5000L,
            25000L,
            4.0,
            96.0,
            null,
            null
        );

        when(analyticsService.getAnalyticsSummary(eq("PRODUCT_LIFECYCLE"), any(), any())).thenReturn(summary);

        mockMvc.perform(get("/api/v1/analytics/summary")
                .param("processType", "PRODUCT_LIFECYCLE"))
            .andExpect(status().isOk())
            .andExpect(jsonPath("$.processType").value("PRODUCT_LIFECYCLE"))
            .andExpect(jsonPath("$.totalProcesses").value(10))
            .andExpect(jsonPath("$.completedProcesses").value(8))
            .andExpect(jsonPath("$.averageCycleTimeMs").value(15000.0));
    }

    @Test
    void shouldReturnThroughputMetrics() throws Exception {
        ThroughputResponse throughput = new ThroughputResponse(
            "PRODUCT_LIFECYCLE",
            24,
            2.0,
            0.08,
            12.0,
            288.0,
            null,
            null
        );

        when(analyticsService.getThroughputAnalytics(eq("PRODUCT_LIFECYCLE"), any(), any())).thenReturn(throughput);

        mockMvc.perform(get("/api/v1/analytics/throughput")
                .param("processType", "PRODUCT_LIFECYCLE"))
            .andExpect(status().isOk())
            .andExpect(jsonPath("$.completedProcesses").value(24))
            .andExpect(jsonPath("$.throughputPerHour").value(12.0))
            .andExpect(jsonPath("$.throughputPerDay").value(288.0));
    }

    @Test
    void shouldReturnBottlenecks() throws Exception {
        BottleneckResponse bottleneck = new BottleneckResponse(
            "INVENTORY_ADJUSTMENT",
            15,
            850.0,
            200,
            1500,
            12750,
            0.65
        );

        when(analyticsService.getBottleneckAnalysis(eq("PRODUCT_LIFECYCLE"), any(), any())).thenReturn(List.of(bottleneck));

        mockMvc.perform(get("/api/v1/analytics/bottlenecks")
                .param("processType", "PRODUCT_LIFECYCLE"))
            .andExpect(status().isOk())
            .andExpect(jsonPath("$[0].stageName").value("INVENTORY_ADJUSTMENT"))
            .andExpect(jsonPath("$[0].averageDurationMs").value(850.0))
            .andExpect(jsonPath("$[0].processTimeContribution").value(0.65));
    }
}
