package com.supplychainx.processservice.analytics;

import com.supplychainx.processservice.analytics.dto.*;
import com.supplychainx.processservice.entity.ProcessEvent;
import com.supplychainx.processservice.entity.ProcessInstance;
import com.supplychainx.processservice.entity.ProcessStep;
import com.supplychainx.processservice.exception.ResourceNotFoundException;
import com.supplychainx.processservice.repository.ProcessEventRepository;
import com.supplychainx.processservice.repository.ProcessInstanceRepository;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.autoconfigure.orm.jpa.DataJpaTest;
import org.springframework.test.context.ActiveProfiles;

import java.time.Instant;
import java.util.List;
import java.util.UUID;

import static org.junit.jupiter.api.Assertions.*;

@DataJpaTest
@ActiveProfiles("test")
class ProcessAnalyticsServiceTests {

    @Autowired
    private ProcessInstanceRepository instanceRepository;

    @Autowired
    private ProcessEventRepository eventRepository;

    @Autowired
    private ProcessAnalyticsRepository analyticsRepository;

    private ProcessAnalyticsService analyticsService;

    @BeforeEach
    void setUp() {
        analyticsService = new ProcessAnalyticsService(analyticsRepository, instanceRepository, eventRepository);
    }

    @Test
    void shouldCalculateSingleProcessMetricsForCompletedProcess() {
        Instant now = Instant.now();
        Instant t1 = now.minusSeconds(300);
        Instant t2 = now;

        ProcessInstance instance = new ProcessInstance("BIZ-AN-01", "PRODUCT_LIFECYCLE", "COMPLETED", t1);
        instance.setCompletedAt(t2);

        ProcessEvent e1 = new ProcessEvent(UUID.randomUUID(), "ProductCreatedEvent", t1, "{}");
        ProcessEvent e2 = new ProcessEvent(UUID.randomUUID(), "ProductDeletedEvent", t2, "{}");
        instance.addEvent(e1);
        instance.addEvent(e2);

        ProcessStep s1 = new ProcessStep("PRODUCT_CREATION", "COMPLETED", t1, t1.plusSeconds(50));
        ProcessStep s2 = new ProcessStep("PRODUCT_DELETION", "COMPLETED", t1.plusSeconds(50), t2);
        instance.addStep(s1);
        instance.addStep(s2);

        ProcessInstance saved = instanceRepository.save(instance);

        ProcessMetricsResponse metrics = analyticsService.getProcessMetrics(saved.getId());

        assertNotNull(metrics);
        assertEquals(saved.getId(), metrics.processInstanceId());
        assertEquals("BIZ-AN-01", metrics.businessKey());
        assertEquals("COMPLETED", metrics.status());
        assertEquals(300000L, metrics.cycleTimeMs());
        assertEquals(2, metrics.eventCount());
        assertEquals(2, metrics.stageCount());
    }

    @Test
    void shouldReturnNullCycleTimeForActiveProcess() {
        Instant t1 = Instant.now().minusSeconds(120);

        ProcessInstance instance = new ProcessInstance("BIZ-ACTIVE-01", "PRODUCT_LIFECYCLE", "ACTIVE", t1);
        ProcessEvent e1 = new ProcessEvent(UUID.randomUUID(), "ProductCreatedEvent", t1, "{}");
        instance.addEvent(e1);

        ProcessInstance saved = instanceRepository.save(instance);

        ProcessMetricsResponse metrics = analyticsService.getProcessMetrics(saved.getId());

        assertNotNull(metrics);
        assertNull(metrics.cycleTimeMs());
        assertTrue(metrics.elapsedDurationMs() >= 120000L);
    }

    @Test
    void shouldThrowNotFoundForInvalidProcessId() {
        UUID randomId = UUID.randomUUID();
        assertThrows(ResourceNotFoundException.class, () -> analyticsService.getProcessMetrics(randomId));
    }

    @Test
    void shouldCalculateStageMetricsCorrectly() {
        Instant t1 = Instant.now().minusSeconds(100);
        Instant t2 = t1.plusSeconds(40);
        Instant t3 = t1.plusSeconds(100);

        ProcessInstance instance = new ProcessInstance("BIZ-STAGE-01", "PRODUCT_LIFECYCLE", "COMPLETED", t1);
        instance.setCompletedAt(t3);

        ProcessStep s1 = new ProcessStep("PRODUCT_CREATION", "COMPLETED", t1, t2);
        ProcessStep s2 = new ProcessStep("PRODUCT_UPDATE", "COMPLETED", t2, t3);
        instance.addStep(s1);
        instance.addStep(s2);

        ProcessInstance saved = instanceRepository.save(instance);

        List<StageMetricsResponse> stages = analyticsService.getProcessStageMetrics(saved.getId());

        assertEquals(2, stages.size());
        assertEquals("PRODUCT_CREATION", stages.get(0).stageName());
        assertEquals(40000L, stages.get(0).durationMs());
        assertEquals("PRODUCT_UPDATE", stages.get(1).stageName());
        assertEquals(60000L, stages.get(1).durationMs());
    }

    @Test
    void shouldCalculateAnalyticsSummaryAndThroughput() {
        Instant t1 = Instant.now().minusSeconds(3600);
        Instant t2 = Instant.now();

        ProcessInstance p1 = new ProcessInstance("BIZ-SUM-1", "PRODUCT_LIFECYCLE", "COMPLETED", t1);
        p1.setCompletedAt(t1.plusSeconds(600));

        ProcessInstance p2 = new ProcessInstance("BIZ-SUM-2", "PRODUCT_LIFECYCLE", "COMPLETED", t1);
        p2.setCompletedAt(t1.plusSeconds(1200));

        ProcessInstance p3 = new ProcessInstance("BIZ-SUM-3", "PRODUCT_LIFECYCLE", "ACTIVE", t1);

        instanceRepository.saveAll(List.of(p1, p2, p3));

        ProcessAnalyticsSummaryResponse summary = analyticsService.getAnalyticsSummary("PRODUCT_LIFECYCLE", t1.minusSeconds(10), t2.plusSeconds(10));

        assertEquals("PRODUCT_LIFECYCLE", summary.processType());
        assertEquals(3, summary.totalProcesses());
        assertEquals(2, summary.completedProcesses());
        assertEquals(1, summary.activeProcesses());
        assertEquals(900000.0, summary.averageCycleTimeMs());
        assertEquals(600000L, summary.minCycleTimeMs());
        assertEquals(1200000L, summary.maxCycleTimeMs());
        assertTrue(summary.throughputPerHour() > 0);
    }

    @Test
    void shouldDetectStageBottlenecksAndCalculateContribution() {
        Instant t1 = Instant.now().minusSeconds(1000);

        ProcessInstance p1 = new ProcessInstance("BIZ-BOT-1", "PRODUCT_LIFECYCLE", "COMPLETED", t1);
        p1.addStep(new ProcessStep("PRODUCT_CREATION", "COMPLETED", t1, t1.plusSeconds(100))); // 100s
        p1.addStep(new ProcessStep("INVENTORY_ADJUSTMENT", "COMPLETED", t1.plusSeconds(100), t1.plusSeconds(400))); // 300s

        ProcessInstance p2 = new ProcessInstance("BIZ-BOT-2", "PRODUCT_LIFECYCLE", "COMPLETED", t1);
        p2.addStep(new ProcessStep("PRODUCT_CREATION", "COMPLETED", t1, t1.plusSeconds(100))); // 100s
        p2.addStep(new ProcessStep("INVENTORY_ADJUSTMENT", "COMPLETED", t1.plusSeconds(100), t1.plusSeconds(500))); // 400s

        instanceRepository.saveAll(List.of(p1, p2));

        List<BottleneckResponse> bottlenecks = analyticsService.getBottleneckAnalysis("PRODUCT_LIFECYCLE", null, null);

        assertEquals(2, bottlenecks.size());
        // INVENTORY_ADJUSTMENT avg = 350s (350,000ms), total = 700s
        // PRODUCT_CREATION avg = 100s (100,000ms), total = 200s
        // Total duration of all stages = 900s
        // INVENTORY_ADJUSTMENT contribution = 700 / 900 = 0.7778
        assertEquals("INVENTORY_ADJUSTMENT", bottlenecks.get(0).stageName());
        assertEquals(350000.0, bottlenecks.get(0).averageDurationMs());
        assertEquals(0.7778, bottlenecks.get(0).processTimeContribution(), 0.001);

        assertEquals("PRODUCT_CREATION", bottlenecks.get(1).stageName());
        assertEquals(100000.0, bottlenecks.get(1).averageDurationMs());
        assertEquals(0.2222, bottlenecks.get(1).processTimeContribution(), 0.001);
    }

    @Test
    void shouldHandleEmptyAnalyticsCleanly() {
        ThroughputResponse throughput = analyticsService.getThroughputAnalytics("UNKNOWN_TYPE", null, null);
        assertEquals(0, throughput.completedProcesses());
        assertEquals(0.0, throughput.throughputPerHour());

        List<BottleneckResponse> bottlenecks = analyticsService.getBottleneckAnalysis("UNKNOWN_TYPE", null, null);
        assertTrue(bottlenecks.isEmpty());
    }

    @Test
    void shouldResolveStageDurationsFromAdjacentStepTimestamps() {
        Instant t1 = Instant.now().minusSeconds(200);
        Instant t2 = t1.plusSeconds(80);
        Instant t3 = t1.plusSeconds(100);

        ProcessInstance instance = new ProcessInstance("BIZ-ADJ-01", "PRODUCT_LIFECYCLE", "COMPLETED", t1);
        instance.setCompletedAt(t3);

        // Steps saved with point-in-time event timestamps (startedAt == completedAt)
        ProcessStep s1 = new ProcessStep("PRODUCT_CREATION", "COMPLETED", t1, t1);
        ProcessStep s2 = new ProcessStep("PRODUCT_UPDATE", "COMPLETED", t2, t2);
        ProcessStep s3 = new ProcessStep("PRODUCT_DELETION", "COMPLETED", t3, t3);

        instance.addStep(s1);
        instance.addStep(s2);
        instance.addStep(s3);

        ProcessInstance saved = instanceRepository.save(instance);

        List<StageMetricsResponse> stages = analyticsService.getProcessStageMetrics(saved.getId());

        assertEquals(3, stages.size());
        assertEquals("PRODUCT_CREATION", stages.get(0).stageName());
        assertEquals(80000L, stages.get(0).durationMs()); // Derived from t2 - t1

        assertEquals("PRODUCT_UPDATE", stages.get(1).stageName());
        assertEquals(20000L, stages.get(1).durationMs()); // Derived from t3 - t2

        assertEquals("PRODUCT_DELETION", stages.get(2).stageName());
        assertEquals(0L, stages.get(2).durationMs()); // Terminal step

        List<BottleneckResponse> bottlenecks = analyticsService.getBottleneckAnalysis("PRODUCT_LIFECYCLE", null, null);
        assertEquals(3, bottlenecks.size());
        assertEquals("PRODUCT_CREATION", bottlenecks.get(0).stageName());
        assertEquals(80000.0, bottlenecks.get(0).averageDurationMs());
        assertEquals(0.8000, bottlenecks.get(0).processTimeContribution(), 0.001);

        assertEquals("PRODUCT_UPDATE", bottlenecks.get(1).stageName());
        assertEquals(20000.0, bottlenecks.get(1).averageDurationMs());
        assertEquals(0.2000, bottlenecks.get(1).processTimeContribution(), 0.001);
    }

    @Test
    void shouldIdentifyDistinctVariantsAndRepeatedActivities() {
        Instant t1 = Instant.now().minusSeconds(500);

        // Variant A: CREATE -> UPDATE -> DELETE (Completed, 100s)
        ProcessInstance p1 = new ProcessInstance("BIZ-VAR-1", "PRODUCT_LIFECYCLE", "COMPLETED", t1);
        p1.setCompletedAt(t1.plusSeconds(100));
        p1.addStep(new ProcessStep("PRODUCT_CREATION", "COMPLETED", t1, t1));
        p1.addStep(new ProcessStep("PRODUCT_UPDATE", "COMPLETED", t1.plusSeconds(40), t1.plusSeconds(40)));
        p1.addStep(new ProcessStep("PRODUCT_DELETION", "COMPLETED", t1.plusSeconds(100), t1.plusSeconds(100)));

        // Variant B: CREATE -> UPDATE -> UPDATE -> DELETE (Completed, 200s) - Repeated activity!
        ProcessInstance p2 = new ProcessInstance("BIZ-VAR-2", "PRODUCT_LIFECYCLE", "COMPLETED", t1);
        p2.setCompletedAt(t1.plusSeconds(200));
        p2.addStep(new ProcessStep("PRODUCT_CREATION", "COMPLETED", t1, t1));
        p2.addStep(new ProcessStep("PRODUCT_UPDATE", "COMPLETED", t1.plusSeconds(50), t1.plusSeconds(50)));
        p2.addStep(new ProcessStep("PRODUCT_UPDATE", "COMPLETED", t1.plusSeconds(120), t1.plusSeconds(120)));
        p2.addStep(new ProcessStep("PRODUCT_DELETION", "COMPLETED", t1.plusSeconds(200), t1.plusSeconds(200)));

        // Variant A Instance 2: CREATE -> UPDATE -> DELETE (Completed, 60s)
        ProcessInstance p3 = new ProcessInstance("BIZ-VAR-3", "PRODUCT_LIFECYCLE", "COMPLETED", t1);
        p3.setCompletedAt(t1.plusSeconds(60));
        p3.addStep(new ProcessStep("PRODUCT_CREATION", "COMPLETED", t1, t1));
        p3.addStep(new ProcessStep("PRODUCT_UPDATE", "COMPLETED", t1.plusSeconds(20), t1.plusSeconds(20)));
        p3.addStep(new ProcessStep("PRODUCT_DELETION", "COMPLETED", t1.plusSeconds(60), t1.plusSeconds(60)));

        instanceRepository.saveAll(List.of(p1, p2, p3));

        List<ProcessVariantResponse> variants = analyticsService.getVariantAnalysis("PRODUCT_LIFECYCLE", null, null);

        assertEquals(2, variants.size());

        // Variant A should have 2 occurrences (66.67%), avg cycle time = (100s + 60s)/2 = 80s (80000ms)
        ProcessVariantResponse varA = variants.get(0);
        assertEquals("PRODUCT_CREATION>PRODUCT_UPDATE>PRODUCT_DELETION", varA.variantKey());
        assertEquals(2, varA.occurrenceCount());
        assertEquals(2, varA.completedCount());
        assertEquals(66.67, varA.percentage());
        assertEquals(80000.0, varA.averageCycleTimeMs());
        assertEquals(60000L, varA.minCycleTimeMs());
        assertEquals(100000L, varA.maxCycleTimeMs());

        // Variant B should have 1 occurrence (33.33%), avg cycle time = 200s (200000ms)
        ProcessVariantResponse varB = variants.get(1);
        assertEquals("PRODUCT_CREATION>PRODUCT_UPDATE>PRODUCT_UPDATE>PRODUCT_DELETION", varB.variantKey());
        assertEquals(1, varB.occurrenceCount());
        assertEquals(1, varB.completedCount());
        assertEquals(33.33, varB.percentage());
        assertEquals(200000.0, varB.averageCycleTimeMs());
    }

    @Test
    void shouldGetVariantByKeyAndThrowNotFoundForUnknown() {
        Instant t1 = Instant.now().minusSeconds(100);
        ProcessInstance p1 = new ProcessInstance("BIZ-VAR-KEY", "PRODUCT_LIFECYCLE", "COMPLETED", t1);
        p1.setCompletedAt(t1.plusSeconds(50));
        p1.addStep(new ProcessStep("PRODUCT_CREATION", "COMPLETED", t1, t1));
        p1.addStep(new ProcessStep("PRODUCT_DELETION", "COMPLETED", t1.plusSeconds(50), t1.plusSeconds(50)));
        instanceRepository.save(p1);

        ProcessVariantResponse found = analyticsService.getVariantByKey("PRODUCT_CREATION>PRODUCT_DELETION", "PRODUCT_LIFECYCLE", null, null);
        assertNotNull(found);
        assertEquals("PRODUCT_CREATION>PRODUCT_DELETION", found.variantKey());

        assertThrows(ResourceNotFoundException.class, () ->
            analyticsService.getVariantByKey("UNKNOWN>KEY", "PRODUCT_LIFECYCLE", null, null)
        );
    }
}
