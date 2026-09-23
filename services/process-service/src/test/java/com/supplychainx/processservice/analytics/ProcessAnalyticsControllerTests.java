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

import com.supplychainx.processservice.metrics.ProcessAnalyticsMetrics;
import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.autoconfigure.web.servlet.WebMvcTest;
import org.springframework.boot.test.mock.mockito.MockBean;
import org.springframework.http.MediaType;
import org.springframework.security.test.context.support.WithMockUser;
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
@WithMockUser(roles = "ANALYST")
class ProcessAnalyticsControllerTests {

    @Autowired
    private MockMvc mockMvc;

    @MockBean
    private ProcessAnalyticsService analyticsService;

    @MockBean
    private ProcessAnalyticsMetrics metrics;
    @MockBean
    private com.supplychainx.processservice.security.JwtTokenProvider jwtTokenProvider;

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

    @Test
    void shouldReturnVariants() throws Exception {
        ProcessVariantResponse variant = new ProcessVariantResponse(
            "PRODUCT_CREATION>PRODUCT_UPDATE>PRODUCT_DELETION",
            "PRODUCT_LIFECYCLE",
            List.of("PRODUCT_CREATION", "PRODUCT_UPDATE", "PRODUCT_DELETION"),
            10,
            66.67,
            10,
            5200.0,
            4100L,
            8400L,
            52000L
        );

        when(analyticsService.getVariantAnalysis(eq("PRODUCT_LIFECYCLE"), any(), any())).thenReturn(List.of(variant));

        mockMvc.perform(get("/api/v1/analytics/variants")
                .param("processType", "PRODUCT_LIFECYCLE"))
            .andExpect(status().isOk())
            .andExpect(jsonPath("$[0].variantKey").value("PRODUCT_CREATION>PRODUCT_UPDATE>PRODUCT_DELETION"))
            .andExpect(jsonPath("$[0].occurrenceCount").value(10))
            .andExpect(jsonPath("$[0].percentage").value(66.67))
            .andExpect(jsonPath("$[0].averageCycleTimeMs").value(5200.0));
    }

    @Test
    void shouldReturnVariantByKeyOr404() throws Exception {
        String key = "PRODUCT_CREATION>PRODUCT_DELETION";
        ProcessVariantResponse variant = new ProcessVariantResponse(
            key,
            "PRODUCT_LIFECYCLE",
            List.of("PRODUCT_CREATION", "PRODUCT_DELETION"),
            5,
            33.33,
            5,
            3000.0,
            2000L,
            4000L,
            15000L
        );

        when(analyticsService.getVariantByKey(eq(key), any(), any(), any())).thenReturn(variant);

        mockMvc.perform(get("/api/v1/analytics/variants/{variantKey}", key))
            .andExpect(status().isOk())
            .andExpect(jsonPath("$.variantKey").value(key))
            .andExpect(jsonPath("$.occurrenceCount").value(5));

        when(analyticsService.getVariantByKey(eq("UNKNOWN"), any(), any(), any()))
            .thenThrow(new ResourceNotFoundException("Process variant not found with key: UNKNOWN"));

        mockMvc.perform(get("/api/v1/analytics/variants/{variantKey}", "UNKNOWN"))
            .andExpect(status().isNotFound());
    }

    @Test
    void shouldReturnReworkSummary() throws Exception {
        ActivityReworkResponse act = new ActivityReworkResponse(
            "PRODUCT_UPDATE",
            10,
            4,
            3,
            1.33,
            100.0
        );

        ReworkAnalyticsSummaryResponse summary = new ReworkAnalyticsSummaryResponse(
            "PRODUCT_LIFECYCLE",
            10,
            4,
            6,
            40.0,
            5,
            1.25,
            25000.0,
            12000.0,
            15000L,
            35000L,
            8000L,
            18000L,
            13000.0,
            List.of(act),
            null,
            null
        );

        when(analyticsService.getReworkAnalyticsSummary(eq("PRODUCT_LIFECYCLE"), any(), any())).thenReturn(summary);

        mockMvc.perform(get("/api/v1/analytics/rework")
                .param("processType", "PRODUCT_LIFECYCLE"))
            .andExpect(status().isOk())
            .andExpect(jsonPath("$.processType").value("PRODUCT_LIFECYCLE"))
            .andExpect(jsonPath("$.totalCompletedProcesses").value(10))
            .andExpect(jsonPath("$.reworkedProcessCount").value(4))
            .andExpect(jsonPath("$.reworkRate").value(40.0))
            .andExpect(jsonPath("$.cycleTimeDifferenceMs").value(13000.0))
            .andExpect(jsonPath("$.activities[0].activityName").value("PRODUCT_UPDATE"));
    }

    @Test
    void shouldReturnProcessReworkDetailOr404() throws Exception {
        UUID processId = UUID.randomUUID();
        ProcessReworkDetailResponse detail = new ProcessReworkDetailResponse(
            processId,
            "BIZ-123",
            "PRODUCT_LIFECYCLE",
            "COMPLETED",
            "PRODUCT_CREATION>PRODUCT_UPDATE>PRODUCT_UPDATE>PRODUCT_DELETION",
            true,
            1,
            java.util.Map.of("PRODUCT_UPDATE", 1L),
            20000L,
            Instant.now()
        );

        when(analyticsService.getProcessReworkDetail(processId)).thenReturn(detail);

        mockMvc.perform(get("/api/v1/analytics/rework/{processId}", processId))
            .andExpect(status().isOk())
            .andExpect(jsonPath("$.processInstanceId").value(processId.toString()))
            .andExpect(jsonPath("$.hasRework").value(true))
            .andExpect(jsonPath("$.totalReworkOccurrences").value(1))
            .andExpect(jsonPath("$.repeatedActivities.PRODUCT_UPDATE").value(1));

        UUID unknownId = UUID.randomUUID();
        when(analyticsService.getProcessReworkDetail(unknownId))
            .thenThrow(new ResourceNotFoundException("ProcessInstance not found with ID: " + unknownId));

        mockMvc.perform(get("/api/v1/analytics/rework/{processId}", unknownId))
            .andExpect(status().isNotFound());
    }

    @Test
    void shouldReturnConformanceSummary() throws Exception {
        ConformanceAnalyticsSummaryResponse summary = new ConformanceAnalyticsSummaryResponse(
            "PRODUCT_LIFECYCLE",
            10,
            8,
            2,
            80.0,
            0.95,
            3,
            java.util.Map.of(
                "MISSING_ACTIVITY", 1L,
                "UNEXPECTED_ACTIVITY", 1L,
                "ORDER_VIOLATION", 1L,
                "TERMINAL_ACTIVITY_VIOLATION", 0L
            ),
            null,
            null
        );

        when(analyticsService.getConformanceAnalyticsSummary(eq("PRODUCT_LIFECYCLE"), any(), any())).thenReturn(summary);

        mockMvc.perform(get("/api/v1/analytics/conformance")
                .param("processType", "PRODUCT_LIFECYCLE"))
            .andExpect(status().isOk())
            .andExpect(jsonPath("$.processType").value("PRODUCT_LIFECYCLE"))
            .andExpect(jsonPath("$.totalProcessesAnalyzed").value(10))
            .andExpect(jsonPath("$.conformantProcessCount").value(8))
            .andExpect(jsonPath("$.conformanceRate").value(80.0))
            .andExpect(jsonPath("$.averageConformanceScore").value(0.95));
    }

    @Test
    void shouldReturnProcessConformanceDetailOr404() throws Exception {
        UUID processId = UUID.randomUUID();
        ProcessConformanceResponse detail = new ProcessConformanceResponse(
            processId,
            "PRODUCT_LIFECYCLE",
            List.of("PRODUCT_CREATION", "PRODUCT_UPDATE", "PRODUCT_DELETION"),
            List.of("PRODUCT_CREATION", "PRODUCT_DELETION"),
            "DEVIATED",
            0.67,
            1,
            List.of(new ProcessDeviationDto("MISSING_ACTIVITY", "PRODUCT_UPDATE", 2, null, "Missing UPDATE")),
            List.of("PRODUCT_UPDATE"),
            List.of(),
            List.of()
        );

        when(analyticsService.getProcessConformanceDetail(processId)).thenReturn(detail);

        mockMvc.perform(get("/api/v1/analytics/conformance/{processId}", processId))
            .andExpect(status().isOk())
            .andExpect(jsonPath("$.processInstanceId").value(processId.toString()))
            .andExpect(jsonPath("$.status").value("DEVIATED"))
            .andExpect(jsonPath("$.conformanceScore").value(0.67))
            .andExpect(jsonPath("$.deviations[0].deviationType").value("MISSING_ACTIVITY"));

        UUID unknownId = UUID.randomUUID();
        when(analyticsService.getProcessConformanceDetail(unknownId))
            .thenThrow(new ResourceNotFoundException("ProcessInstance not found with ID: " + unknownId));

        mockMvc.perform(get("/api/v1/analytics/conformance/{processId}", unknownId))
            .andExpect(status().isNotFound());
    }
}
