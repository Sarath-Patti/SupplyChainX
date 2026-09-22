package com.supplychainx.processservice.analytics;

import com.supplychainx.processservice.analytics.dto.*;
import com.supplychainx.processservice.entity.ProcessEvent;
import com.supplychainx.processservice.entity.ProcessInstance;
import com.supplychainx.processservice.entity.ProcessStep;
import com.supplychainx.processservice.exception.ResourceNotFoundException;
import com.supplychainx.processservice.repository.ProcessEventRepository;
import com.supplychainx.processservice.repository.ProcessInstanceRepository;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.time.Duration;
import java.time.Instant;
import java.util.*;
import java.util.stream.Collectors;

@Service
@Transactional(readOnly = true)
public class ProcessAnalyticsService {

    private final ProcessAnalyticsRepository analyticsRepository;
    private final ProcessInstanceRepository instanceRepository;
    private final ProcessEventRepository eventRepository;

    public ProcessAnalyticsService(
        ProcessAnalyticsRepository analyticsRepository,
        ProcessInstanceRepository instanceRepository,
        ProcessEventRepository eventRepository) {
        this.analyticsRepository = analyticsRepository;
        this.instanceRepository = instanceRepository;
        this.eventRepository = eventRepository;
    }

    public ProcessMetricsResponse getProcessMetrics(UUID processInstanceId) {
        ProcessInstance instance = instanceRepository.findById(processInstanceId)
            .orElseThrow(() -> new ResourceNotFoundException("ProcessInstance not found with ID: " + processInstanceId));

        List<ProcessEvent> events = eventRepository.findByProcessInstanceIdOrderByTimestampAsc(processInstanceId);
        int eventCount = events.size();
        int stageCount = instance.getSteps().size();

        Instant startedAt = instance.getStartedAt();
        Instant completedAt = instance.getCompletedAt();

        // Calculate cycle time if completed
        Long cycleTimeMs = null;
        if (completedAt != null && startedAt != null) {
            cycleTimeMs = Math.max(0L, Duration.between(startedAt, completedAt).toMillis());
        } else if (!events.isEmpty() && "COMPLETED".equalsIgnoreCase(instance.getStatus())) {
            Instant firstEventTime = events.get(0).getTimestamp();
            Instant lastEventTime = events.get(events.size() - 1).getTimestamp();
            cycleTimeMs = Math.max(0L, Duration.between(firstEventTime, lastEventTime).toMillis());
        }

        // Elapsed duration
        Instant endTime = completedAt != null ? completedAt : Instant.now();
        Long elapsedDurationMs = startedAt != null ? Math.max(0L, Duration.between(startedAt, endTime).toMillis()) : 0L;

        return new ProcessMetricsResponse(
            instance.getId(),
            instance.getBusinessKey(),
            instance.getProcessType(),
            instance.getStatus(),
            cycleTimeMs,
            elapsedDurationMs,
            stageCount,
            eventCount,
            startedAt,
            completedAt
        );
    }

    public List<StageMetricsResponse> getProcessStageMetrics(UUID processInstanceId) {
        ProcessInstance instance = instanceRepository.findById(processInstanceId)
            .orElseThrow(() -> new ResourceNotFoundException("ProcessInstance not found with ID: " + processInstanceId));

        List<ProcessStep> sortedSteps = instance.getSteps().stream()
            .sorted(Comparator.comparing(ProcessStep::getStartedAt, Comparator.nullsLast(Comparator.naturalOrder())))
            .toList();

        List<StageMetricsResponse> responses = new ArrayList<>();
        for (int i = 0; i < sortedSteps.size(); i++) {
            ProcessStep step = sortedSteps.get(i);
            Instant startedAt = step.getStartedAt();
            Instant completedAt = resolveEffectiveCompletedAt(step, i, sortedSteps, instance);

            Long durationMs = null;
            if (startedAt != null && completedAt != null) {
                durationMs = Math.max(0L, Duration.between(startedAt, completedAt).toMillis());
            }

            responses.add(new StageMetricsResponse(
                step.getId(),
                instance.getId(),
                step.getStepName(),
                step.getStatus(),
                startedAt,
                completedAt,
                durationMs
            ));
        }

        return responses;
    }

    public ProcessAnalyticsSummaryResponse getAnalyticsSummary(String processType, Instant from, Instant to) {
        List<ProcessInstance> allInstances = analyticsRepository.findInstancesForAnalytics(processType, from, to);
        List<ProcessInstance> completedInstances = analyticsRepository.findCompletedInstancesForAnalytics(processType, from, to);

        long totalProcesses = allInstances.size();
        long completedProcesses = completedInstances.size();
        long activeProcesses = totalProcesses - completedProcesses;

        List<Long> cycleTimes = completedInstances.stream()
            .map(p -> {
                if (p.getStartedAt() != null && p.getCompletedAt() != null) {
                    return Math.max(0L, Duration.between(p.getStartedAt(), p.getCompletedAt()).toMillis());
                }
                return null;
            })
            .filter(Objects::nonNull)
            .toList();

        Double avgCycleTimeMs = cycleTimes.isEmpty() ? null : cycleTimes.stream().mapToLong(Long::longValue).average().orElse(0.0);
        Long minCycleTimeMs = cycleTimes.isEmpty() ? null : cycleTimes.stream().mapToLong(Long::longValue).min().orElse(0L);
        Long maxCycleTimeMs = cycleTimes.isEmpty() ? null : cycleTimes.stream().mapToLong(Long::longValue).max().orElse(0L);

        ThroughputResponse throughput = getThroughputAnalytics(processType, from, to);

        return new ProcessAnalyticsSummaryResponse(
            processType != null ? processType : "ALL_TYPES",
            totalProcesses,
            completedProcesses,
            activeProcesses,
            avgCycleTimeMs != null ? Math.round(avgCycleTimeMs * 100.0) / 100.0 : null,
            minCycleTimeMs,
            maxCycleTimeMs,
            throughput.throughputPerHour(),
            throughput.throughputPerDay(),
            from,
            to
        );
    }

    public ThroughputResponse getThroughputAnalytics(String processType, Instant from, Instant to) {
        List<ProcessInstance> completedInstances = analyticsRepository.findCompletedInstancesForAnalytics(processType, from, to);
        long completedCount = completedInstances.size();

        if (completedCount == 0) {
            return new ThroughputResponse(
                processType != null ? processType : "ALL_TYPES",
                0,
                0.0,
                0.0,
                0.0,
                0.0,
                from,
                to
            );
        }

        Instant startTime = from;
        Instant endTime = to;

        if (startTime == null) {
            startTime = completedInstances.stream()
                .map(ProcessInstance::getStartedAt)
                .filter(Objects::nonNull)
                .min(Comparator.naturalOrder())
                .orElse(Instant.now().minus(Duration.ofHours(1)));
        }

        if (endTime == null) {
            endTime = completedInstances.stream()
                .map(ProcessInstance::getCompletedAt)
                .filter(Objects::nonNull)
                .max(Comparator.naturalOrder())
                .orElse(Instant.now());
        }

        long millis = Math.max(1000L, Duration.between(startTime, endTime).toMillis());
        double hours = millis / 3600000.0;
        double days = hours / 24.0;

        double throughputPerHour = Math.round((completedCount / hours) * 100.0) / 100.0;
        double throughputPerDay = Math.round((completedCount / (days > 0 ? days : 1.0 / 24.0)) * 100.0) / 100.0;

        return new ThroughputResponse(
            processType != null ? processType : "ALL_TYPES",
            completedCount,
            Math.round(hours * 100.0) / 100.0,
            Math.round(days * 100.0) / 100.0,
            throughputPerHour,
            throughputPerDay,
            from,
            to
        );
    }

    public List<BottleneckResponse> getBottleneckAnalysis(String processType, Instant from, Instant to) {
        List<ProcessInstance> instances = analyticsRepository.findInstancesForAnalytics(processType, from, to);

        if (instances.isEmpty()) {
            return Collections.emptyList();
        }

        Map<String, List<Long>> durationsByStage = new HashMap<>();
        Map<String, Long> countByStage = new HashMap<>();

        for (ProcessInstance instance : instances) {
            List<ProcessStep> sortedSteps = instance.getSteps().stream()
                .sorted(Comparator.comparing(ProcessStep::getStartedAt, Comparator.nullsLast(Comparator.naturalOrder())))
                .toList();

            for (int i = 0; i < sortedSteps.size(); i++) {
                ProcessStep step = sortedSteps.get(i);
                String stageName = step.getStepName();
                countByStage.put(stageName, countByStage.getOrDefault(stageName, 0L) + 1);

                Instant startedAt = step.getStartedAt();
                Instant completedAt = resolveEffectiveCompletedAt(step, i, sortedSteps, instance);

                if (startedAt != null && completedAt != null && !completedAt.isBefore(startedAt)) {
                    long durationMs = Duration.between(startedAt, completedAt).toMillis();
                    durationsByStage.computeIfAbsent(stageName, k -> new ArrayList<>()).add(durationMs);
                }
            }
        }

        if (countByStage.isEmpty()) {
            return Collections.emptyList();
        }

        long grandTotalDurationMs = 0L;
        List<StageDurationStats> statsList = new ArrayList<>();

        for (Map.Entry<String, Long> entry : countByStage.entrySet()) {
            String stageName = entry.getKey();
            long count = entry.getValue();
            List<Long> durations = durationsByStage.getOrDefault(stageName, Collections.emptyList());

            double avg = durations.isEmpty() ? 0.0 : durations.stream().mapToLong(Long::longValue).average().orElse(0.0);
            long min = durations.isEmpty() ? 0L : durations.stream().mapToLong(Long::longValue).min().orElse(0L);
            long max = durations.isEmpty() ? 0L : durations.stream().mapToLong(Long::longValue).max().orElse(0L);
            long sum = durations.isEmpty() ? 0L : durations.stream().mapToLong(Long::longValue).sum();

            grandTotalDurationMs += sum;

            statsList.add(new StageDurationStats(stageName, count, avg, min, max, sum));
        }

        final long totalAllStages = grandTotalDurationMs;

        return statsList.stream()
            .map(stat -> {
                double contribution = totalAllStages > 0
                    ? Math.round((stat.totalDurationMs() / (double) totalAllStages) * 10000.0) / 10000.0
                    : 0.0;
                return new BottleneckResponse(
                    stat.stageName(),
                    stat.executionCount(),
                    Math.round(stat.averageDurationMs() * 100.0) / 100.0,
                    stat.minDurationMs(),
                    stat.maxDurationMs(),
                    stat.totalDurationMs(),
                    contribution
                );
            })
            .sorted(Comparator.comparing(BottleneckResponse::averageDurationMs).reversed())
            .toList();
    }

    private Instant resolveEffectiveCompletedAt(ProcessStep step, int index, List<ProcessStep> sortedSteps, ProcessInstance instance) {
        Instant startedAt = step.getStartedAt();
        Instant completedAt = step.getCompletedAt();

        if (startedAt != null && (completedAt == null || completedAt.equals(startedAt))) {
            if (index < sortedSteps.size() - 1) {
                return sortedSteps.get(index + 1).getStartedAt();
            } else if (instance.getCompletedAt() != null) {
                return instance.getCompletedAt();
            }
        }
        return completedAt;
    }
}
