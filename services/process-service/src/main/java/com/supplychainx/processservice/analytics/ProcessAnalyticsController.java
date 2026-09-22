package com.supplychainx.processservice.analytics;

import com.supplychainx.processservice.analytics.dto.*;
import org.springframework.format.annotation.DateTimeFormat;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;

import java.time.Instant;
import java.util.List;
import java.util.UUID;

@RestController
@RequestMapping("/api/v1/analytics")
public class ProcessAnalyticsController {

    private final ProcessAnalyticsService analyticsService;

    public ProcessAnalyticsController(ProcessAnalyticsService analyticsService) {
        this.analyticsService = analyticsService;
    }

    @GetMapping("/processes/{id}")
    public ResponseEntity<ProcessMetricsResponse> getProcessMetrics(@PathVariable UUID id) {
        ProcessMetricsResponse metrics = analyticsService.getProcessMetrics(id);
        return ResponseEntity.ok(metrics);
    }

    @GetMapping("/processes/{id}/stages")
    public ResponseEntity<List<StageMetricsResponse>> getProcessStageMetrics(@PathVariable UUID id) {
        List<StageMetricsResponse> stages = analyticsService.getProcessStageMetrics(id);
        return ResponseEntity.ok(stages);
    }

    @GetMapping("/summary")
    public ResponseEntity<ProcessAnalyticsSummaryResponse> getAnalyticsSummary(
        @RequestParam(required = false) String processType,
        @RequestParam(required = false) @DateTimeFormat(iso = DateTimeFormat.ISO.DATE_TIME) Instant from,
        @RequestParam(required = false) @DateTimeFormat(iso = DateTimeFormat.ISO.DATE_TIME) Instant to) {

        ProcessAnalyticsSummaryResponse summary = analyticsService.getAnalyticsSummary(processType, from, to);
        return ResponseEntity.ok(summary);
    }

    @GetMapping("/throughput")
    public ResponseEntity<ThroughputResponse> getThroughput(
        @RequestParam(required = false) String processType,
        @RequestParam(required = false) @DateTimeFormat(iso = DateTimeFormat.ISO.DATE_TIME) Instant from,
        @RequestParam(required = false) @DateTimeFormat(iso = DateTimeFormat.ISO.DATE_TIME) Instant to) {

        ThroughputResponse throughput = analyticsService.getThroughputAnalytics(processType, from, to);
        return ResponseEntity.ok(throughput);
    }

    @GetMapping("/bottlenecks")
    public ResponseEntity<List<BottleneckResponse>> getBottlenecks(
        @RequestParam(required = false) String processType,
        @RequestParam(required = false) @DateTimeFormat(iso = DateTimeFormat.ISO.DATE_TIME) Instant from,
        @RequestParam(required = false) @DateTimeFormat(iso = DateTimeFormat.ISO.DATE_TIME) Instant to) {

        List<BottleneckResponse> bottlenecks = analyticsService.getBottleneckAnalysis(processType, from, to);
        return ResponseEntity.ok(bottlenecks);
    }
}
