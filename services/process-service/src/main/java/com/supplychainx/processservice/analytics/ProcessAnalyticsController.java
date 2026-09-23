package com.supplychainx.processservice.analytics;

import com.supplychainx.processservice.analytics.dto.*;
import com.supplychainx.processservice.metrics.ProcessAnalyticsMetrics;
import org.springframework.format.annotation.DateTimeFormat;
import org.springframework.http.ResponseEntity;
import org.springframework.security.access.prepost.PreAuthorize;
import org.springframework.web.bind.annotation.*;

import java.time.Instant;
import java.util.List;
import java.util.Map;
import java.util.UUID;

@RestController
@RequestMapping("/api/v1/analytics")
public class ProcessAnalyticsController {

    private final ProcessAnalyticsService analyticsService;
    private final ProcessAnalyticsMetrics metrics;

    public ProcessAnalyticsController(ProcessAnalyticsService analyticsService, ProcessAnalyticsMetrics metrics) {
        this.analyticsService = analyticsService;
        this.metrics = metrics;
    }

    @GetMapping("/processes/{id}")
    public ResponseEntity<ProcessMetricsResponse> getProcessMetrics(@PathVariable UUID id) {
        metrics.incrementAnalyticsRequest("/api/v1/analytics/processes/{id}", "GET", 200);
        ProcessMetricsResponse response = analyticsService.getProcessMetrics(id);
        return ResponseEntity.ok(response);
    }

    @GetMapping("/processes/{id}/stages")
    public ResponseEntity<List<StageMetricsResponse>> getProcessStageMetrics(@PathVariable UUID id) {
        metrics.incrementAnalyticsRequest("/api/v1/analytics/processes/{id}/stages", "GET", 200);
        List<StageMetricsResponse> stages = analyticsService.getProcessStageMetrics(id);
        return ResponseEntity.ok(stages);
    }

    @GetMapping("/summary")
    public ResponseEntity<ProcessAnalyticsSummaryResponse> getAnalyticsSummary(
        @RequestParam(required = false) String processType,
        @RequestParam(required = false) @DateTimeFormat(iso = DateTimeFormat.ISO.DATE_TIME) Instant from,
        @RequestParam(required = false) @DateTimeFormat(iso = DateTimeFormat.ISO.DATE_TIME) Instant to) {

        metrics.incrementAnalyticsRequest("/api/v1/analytics/summary", "GET", 200);
        ProcessAnalyticsSummaryResponse summary = analyticsService.getAnalyticsSummary(processType, from, to);
        return ResponseEntity.ok(summary);
    }

    @GetMapping("/throughput")
    public ResponseEntity<ThroughputResponse> getThroughput(
        @RequestParam(required = false) String processType,
        @RequestParam(required = false) @DateTimeFormat(iso = DateTimeFormat.ISO.DATE_TIME) Instant from,
        @RequestParam(required = false) @DateTimeFormat(iso = DateTimeFormat.ISO.DATE_TIME) Instant to) {

        metrics.incrementAnalyticsRequest("/api/v1/analytics/throughput", "GET", 200);
        ThroughputResponse throughput = analyticsService.getThroughputAnalytics(processType, from, to);
        return ResponseEntity.ok(throughput);
    }

    @GetMapping("/bottlenecks")
    public ResponseEntity<List<BottleneckResponse>> getBottlenecks(
        @RequestParam(required = false) String processType,
        @RequestParam(required = false) @DateTimeFormat(iso = DateTimeFormat.ISO.DATE_TIME) Instant from,
        @RequestParam(required = false) @DateTimeFormat(iso = DateTimeFormat.ISO.DATE_TIME) Instant to) {

        metrics.incrementAnalyticsRequest("/api/v1/analytics/bottlenecks", "GET", 200);
        List<BottleneckResponse> bottlenecks = analyticsService.getBottleneckAnalysis(processType, from, to);
        return ResponseEntity.ok(bottlenecks);
    }

    @GetMapping("/variants")
    public ResponseEntity<List<ProcessVariantResponse>> getVariants(
        @RequestParam(required = false) String processType,
        @RequestParam(required = false) @DateTimeFormat(iso = DateTimeFormat.ISO.DATE_TIME) Instant from,
        @RequestParam(required = false) @DateTimeFormat(iso = DateTimeFormat.ISO.DATE_TIME) Instant to) {

        metrics.incrementAnalyticsRequest("/api/v1/analytics/variants", "GET", 200);
        metrics.incrementVariantsRequest("200");
        List<ProcessVariantResponse> variants = analyticsService.getVariantAnalysis(processType, from, to);
        return ResponseEntity.ok(variants);
    }

    @GetMapping("/variants/{variantKey:.+}")
    public ResponseEntity<ProcessVariantResponse> getVariantByKey(
        @PathVariable String variantKey,
        @RequestParam(required = false) String processType,
        @RequestParam(required = false) @DateTimeFormat(iso = DateTimeFormat.ISO.DATE_TIME) Instant from,
        @RequestParam(required = false) @DateTimeFormat(iso = DateTimeFormat.ISO.DATE_TIME) Instant to) {

        metrics.incrementAnalyticsRequest("/api/v1/analytics/variants/{variantKey}", "GET", 200);
        metrics.incrementVariantsRequest("200");
        ProcessVariantResponse variant = analyticsService.getVariantByKey(variantKey, processType, from, to);
        return ResponseEntity.ok(variant);
    }

    @GetMapping("/rework")
    public ResponseEntity<ReworkAnalyticsSummaryResponse> getReworkAnalytics(
        @RequestParam(required = false) String processType,
        @RequestParam(required = false) @DateTimeFormat(iso = DateTimeFormat.ISO.DATE_TIME) Instant from,
        @RequestParam(required = false) @DateTimeFormat(iso = DateTimeFormat.ISO.DATE_TIME) Instant to) {

        metrics.incrementAnalyticsRequest("/api/v1/analytics/rework", "GET", 200);
        metrics.incrementReworkRequest("200");
        ReworkAnalyticsSummaryResponse reworkSummary = analyticsService.getReworkAnalyticsSummary(processType, from, to);
        return ResponseEntity.ok(reworkSummary);
    }

    @GetMapping("/rework/{processId}")
    public ResponseEntity<ProcessReworkDetailResponse> getProcessReworkDetail(@PathVariable UUID processId) {
        metrics.incrementAnalyticsRequest("/api/v1/analytics/rework/{processId}", "GET", 200);
        metrics.incrementReworkRequest("200");
        ProcessReworkDetailResponse reworkDetail = analyticsService.getProcessReworkDetail(processId);
        return ResponseEntity.ok(reworkDetail);
    }

    @GetMapping("/conformance")
    public ResponseEntity<ConformanceAnalyticsSummaryResponse> getConformanceAnalytics(
        @RequestParam(required = false) String processType,
        @RequestParam(required = false) @DateTimeFormat(iso = DateTimeFormat.ISO.DATE_TIME) Instant from,
        @RequestParam(required = false) @DateTimeFormat(iso = DateTimeFormat.ISO.DATE_TIME) Instant to) {

        metrics.incrementAnalyticsRequest("/api/v1/analytics/conformance", "GET", 200);
        metrics.incrementConformanceRequest("200");
        ConformanceAnalyticsSummaryResponse summary = analyticsService.getConformanceAnalyticsSummary(processType, from, to);
        return ResponseEntity.ok(summary);
    }

    @GetMapping("/conformance/{processId}")
    public ResponseEntity<ProcessConformanceResponse> getProcessConformanceDetail(@PathVariable UUID processId) {
        metrics.incrementAnalyticsRequest("/api/v1/analytics/conformance/{processId}", "GET", 200);
        metrics.incrementConformanceRequest("200");
        ProcessConformanceResponse response = analyticsService.getProcessConformanceDetail(processId);
        return ResponseEntity.ok(response);
    }

    @GetMapping("/admin/summary")
    @PreAuthorize("hasRole('ADMIN')")
    public ResponseEntity<Map<String, Object>> getAdminSummary() {
        metrics.incrementAnalyticsRequest("/api/v1/analytics/admin/summary", "GET", 200);
        Map<String, Object> adminSummary = Map.of(
            "status", "ACTIVE",
            "securityLevel", "ENTERPRISE_HIGH",
            "managedService", "process-service",
            "timestamp", Instant.now().toString()
        );
        return ResponseEntity.ok(adminSummary);
    }
}
