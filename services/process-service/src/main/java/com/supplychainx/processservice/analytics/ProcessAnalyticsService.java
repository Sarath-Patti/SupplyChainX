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

    public List<ProcessVariantResponse> getVariantAnalysis(String processType, Instant from, Instant to) {
        List<ProcessInstance> instances = analyticsRepository.findInstancesForAnalytics(processType, from, to);

        if (instances.isEmpty()) {
            return Collections.emptyList();
        }

        Map<String, List<ProcessInstance>> variantGroups = new LinkedHashMap<>();
        Map<String, List<String>> sequencesByKey = new HashMap<>();

        for (ProcessInstance instance : instances) {
            List<ProcessStep> sortedSteps = instance.getSteps().stream()
                .sorted(Comparator.comparing(ProcessStep::getStartedAt, Comparator.nullsLast(Comparator.naturalOrder())))
                .toList();

            List<String> sequence = sortedSteps.stream()
                .map(ProcessStep::getStepName)
                .toList();

            if (sequence.isEmpty()) {
                sequence = List.of("UNKNOWN_STAGE");
            }

            String variantKey = String.join(">", sequence);
            variantGroups.computeIfAbsent(variantKey, k -> new ArrayList<>()).add(instance);
            sequencesByKey.putIfAbsent(variantKey, sequence);
        }

        long totalCompletedProcesses = instances.stream()
            .filter(p -> "COMPLETED".equalsIgnoreCase(p.getStatus()))
            .count();

        List<ProcessVariantResponse> variantResponses = new ArrayList<>();

        for (Map.Entry<String, List<ProcessInstance>> entry : variantGroups.entrySet()) {
            String variantKey = entry.getKey();
            List<ProcessInstance> groupInstances = entry.getValue();
            List<String> sequence = sequencesByKey.get(variantKey);

            long occurrenceCount = groupInstances.size();

            List<ProcessInstance> completedInGroup = groupInstances.stream()
                .filter(p -> "COMPLETED".equalsIgnoreCase(p.getStatus()) && p.getStartedAt() != null && p.getCompletedAt() != null)
                .toList();

            long completedCount = completedInGroup.size();

            List<Long> cycleTimes = completedInGroup.stream()
                .map(p -> Math.max(0L, Duration.between(p.getStartedAt(), p.getCompletedAt()).toMillis()))
                .toList();

            Double avgCycleTimeMs = cycleTimes.isEmpty() ? null : cycleTimes.stream().mapToLong(Long::longValue).average().orElse(0.0);
            Long minCycleTimeMs = cycleTimes.isEmpty() ? null : cycleTimes.stream().mapToLong(Long::longValue).min().orElse(0L);
            Long maxCycleTimeMs = cycleTimes.isEmpty() ? null : cycleTimes.stream().mapToLong(Long::longValue).max().orElse(0L);
            Long totalCycleTimeMs = cycleTimes.isEmpty() ? null : cycleTimes.stream().mapToLong(Long::longValue).sum();

            double percentage = totalCompletedProcesses > 0
                ? Math.round(((double) completedCount / totalCompletedProcesses) * 10000.0) / 100.0
                : 0.0;

            String instanceType = processType != null ? processType : groupInstances.get(0).getProcessType();

            variantResponses.add(new ProcessVariantResponse(
                variantKey,
                instanceType,
                sequence,
                occurrenceCount,
                percentage,
                completedCount,
                avgCycleTimeMs != null ? Math.round(avgCycleTimeMs * 100.0) / 100.0 : null,
                minCycleTimeMs,
                maxCycleTimeMs,
                totalCycleTimeMs
            ));
        }

        return variantResponses.stream()
            .sorted(Comparator.comparing(ProcessVariantResponse::occurrenceCount).reversed()
                .thenComparing(ProcessVariantResponse::completedCount, Comparator.reverseOrder()))
            .toList();
    }

    public ProcessVariantResponse getVariantByKey(String variantKey, String processType, Instant from, Instant to) {
        String decodedKey = java.net.URLDecoder.decode(variantKey, java.nio.charset.StandardCharsets.UTF_8);
        return getVariantAnalysis(processType, from, to).stream()
            .filter(v -> v.variantKey().equalsIgnoreCase(variantKey) || v.variantKey().equalsIgnoreCase(decodedKey))
            .findFirst()
            .orElseThrow(() -> new ResourceNotFoundException("Process variant not found with key: " + variantKey));
    }

    public ReworkAnalyticsSummaryResponse getReworkAnalyticsSummary(String processType, Instant from, Instant to) {
        List<ProcessInstance> completedInstances = analyticsRepository.findCompletedInstancesForAnalytics(processType, from, to);

        long totalCompletedProcesses = completedInstances.size();
        if (totalCompletedProcesses == 0) {
            return new ReworkAnalyticsSummaryResponse(
                processType != null ? processType : "ALL_TYPES",
                0L,
                0L,
                0L,
                0.0,
                0L,
                0.0,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                Collections.emptyList(),
                from,
                to
            );
        }

        long reworkedProcessCount = 0;
        long nonReworkedProcessCount = 0;
        long totalReworkOccurrences = 0;

        List<Long> cycleTimesWithRework = new ArrayList<>();
        List<Long> cycleTimesWithoutRework = new ArrayList<>();

        Map<String, Long> globalExecutionsByActivity = new HashMap<>();
        Map<String, Long> globalReworkByActivity = new HashMap<>();
        Map<String, Long> affectedProcessesByActivity = new HashMap<>();

        for (ProcessInstance instance : completedInstances) {
            List<String> sequence = extractActivitySequence(instance);

            Map<String, Long> activityCounts = sequence.stream()
                .collect(Collectors.groupingBy(s -> s, Collectors.counting()));

            long processReworkCount = 0;

            for (Map.Entry<String, Long> entry : activityCounts.entrySet()) {
                String activity = entry.getKey();
                long execCount = entry.getValue();

                globalExecutionsByActivity.put(activity, globalExecutionsByActivity.getOrDefault(activity, 0L) + execCount);

                long actRework = Math.max(0L, execCount - 1L);
                if (actRework > 0) {
                    globalReworkByActivity.put(activity, globalReworkByActivity.getOrDefault(activity, 0L) + actRework);
                    affectedProcessesByActivity.put(activity, affectedProcessesByActivity.getOrDefault(activity, 0L) + 1L);
                    processReworkCount += actRework;
                }
            }

            totalReworkOccurrences += processReworkCount;

            Long cycleTimeMs = null;
            if (instance.getStartedAt() != null && instance.getCompletedAt() != null) {
                cycleTimeMs = Math.max(0L, Duration.between(instance.getStartedAt(), instance.getCompletedAt()).toMillis());
            }

            if (processReworkCount > 0) {
                reworkedProcessCount++;
                if (cycleTimeMs != null) {
                    cycleTimesWithRework.add(cycleTimeMs);
                }
            } else {
                nonReworkedProcessCount++;
                if (cycleTimeMs != null) {
                    cycleTimesWithoutRework.add(cycleTimeMs);
                }
            }
        }

        double reworkRate = Math.round(((double) reworkedProcessCount / totalCompletedProcesses * 100.0) * 100.0) / 100.0;
        double avgReworkPerReworked = reworkedProcessCount > 0
            ? Math.round(((double) totalReworkOccurrences / reworkedProcessCount) * 100.0) / 100.0
            : 0.0;

        Double avgCycleTimeWithReworkMs = cycleTimesWithRework.isEmpty() ? null
            : Math.round(cycleTimesWithRework.stream().mapToLong(Long::longValue).average().orElse(0.0) * 100.0) / 100.0;
        Long minCycleTimeWithReworkMs = cycleTimesWithRework.isEmpty() ? null
            : cycleTimesWithRework.stream().mapToLong(Long::longValue).min().orElse(0L);
        Long maxCycleTimeWithReworkMs = cycleTimesWithRework.isEmpty() ? null
            : cycleTimesWithRework.stream().mapToLong(Long::longValue).max().orElse(0L);

        Double avgCycleTimeWithoutReworkMs = cycleTimesWithoutRework.isEmpty() ? null
            : Math.round(cycleTimesWithoutRework.stream().mapToLong(Long::longValue).average().orElse(0.0) * 100.0) / 100.0;
        Long minCycleTimeWithoutReworkMs = cycleTimesWithoutRework.isEmpty() ? null
            : cycleTimesWithoutRework.stream().mapToLong(Long::longValue).min().orElse(0L);
        Long maxCycleTimeWithoutReworkMs = cycleTimesWithoutRework.isEmpty() ? null
            : cycleTimesWithoutRework.stream().mapToLong(Long::longValue).max().orElse(0L);

        Double cycleTimeDifferenceMs = (avgCycleTimeWithReworkMs != null && avgCycleTimeWithoutReworkMs != null)
            ? Math.round((avgCycleTimeWithReworkMs - avgCycleTimeWithoutReworkMs) * 100.0) / 100.0
            : null;

        final long finalTotalReworkOccurrences = totalReworkOccurrences;

        List<ActivityReworkResponse> activityResponses = globalReworkByActivity.entrySet().stream()
            .map(entry -> {
                String activity = entry.getKey();
                long reworkCount = entry.getValue();
                long totalExec = globalExecutionsByActivity.getOrDefault(activity, 0L);
                long affectedProc = affectedProcessesByActivity.getOrDefault(activity, 0L);

                double avgPerAffected = affectedProc > 0
                    ? Math.round(((double) reworkCount / affectedProc) * 100.0) / 100.0
                    : 0.0;
                double contributionPct = finalTotalReworkOccurrences > 0
                    ? Math.round(((double) reworkCount / finalTotalReworkOccurrences * 100.0) * 100.0) / 100.0
                    : 0.0;

                return new ActivityReworkResponse(
                    activity,
                    totalExec,
                    reworkCount,
                    affectedProc,
                    avgPerAffected,
                    contributionPct
                );
            })
            .sorted(Comparator.comparing(ActivityReworkResponse::reworkOccurrences).reversed()
                .thenComparing(ActivityReworkResponse::totalExecutionCount, Comparator.reverseOrder()))
            .toList();

        return new ReworkAnalyticsSummaryResponse(
            processType != null ? processType : "ALL_TYPES",
            totalCompletedProcesses,
            reworkedProcessCount,
            nonReworkedProcessCount,
            reworkRate,
            totalReworkOccurrences,
            avgReworkPerReworked,
            avgCycleTimeWithReworkMs,
            avgCycleTimeWithoutReworkMs,
            minCycleTimeWithReworkMs,
            maxCycleTimeWithReworkMs,
            minCycleTimeWithoutReworkMs,
            maxCycleTimeWithoutReworkMs,
            cycleTimeDifferenceMs,
            activityResponses,
            from,
            to
        );
    }

    public ProcessReworkDetailResponse getProcessReworkDetail(UUID processInstanceId) {
        ProcessInstance instance = instanceRepository.findById(processInstanceId)
            .orElseThrow(() -> new ResourceNotFoundException("ProcessInstance not found with ID: " + processInstanceId));

        List<String> sequence = extractActivitySequence(instance);
        String variantKey = String.join(">", sequence.isEmpty() ? List.of("UNKNOWN_STAGE") : sequence);

        Map<String, Long> activityCounts = sequence.stream()
            .collect(Collectors.groupingBy(s -> s, LinkedHashMap::new, Collectors.counting()));

        Map<String, Long> repeatedActivities = new LinkedHashMap<>();
        long totalReworkOccurrences = 0;

        for (Map.Entry<String, Long> entry : activityCounts.entrySet()) {
            long rework = Math.max(0L, entry.getValue() - 1L);
            if (rework > 0) {
                repeatedActivities.put(entry.getKey(), rework);
                totalReworkOccurrences += rework;
            }
        }

        boolean hasRework = totalReworkOccurrences > 0;

        Long cycleTimeMs = null;
        if (instance.getStartedAt() != null && instance.getCompletedAt() != null) {
            cycleTimeMs = Math.max(0L, Duration.between(instance.getStartedAt(), instance.getCompletedAt()).toMillis());
        }

        return new ProcessReworkDetailResponse(
            instance.getId(),
            instance.getBusinessKey(),
            instance.getProcessType(),
            instance.getStatus(),
            variantKey,
            hasRework,
            totalReworkOccurrences,
            repeatedActivities,
            cycleTimeMs,
            instance.getCompletedAt()
        );
    }

    public List<String> getExpectedSequence(String processType) {
        if (processType == null) {
            return List.of("PRODUCT_CREATION", "PRODUCT_UPDATE", "PRODUCT_DELETION");
        }
        return switch (processType.toUpperCase()) {
            case "WAREHOUSE_LIFECYCLE" -> List.of("WAREHOUSE_CREATION", "WAREHOUSE_UPDATE", "WAREHOUSE_DELETION");
            case "INVENTORY_MANAGEMENT" -> List.of("INVENTORY_ADJUSTMENT");
            default -> List.of("PRODUCT_CREATION", "PRODUCT_UPDATE", "PRODUCT_DELETION");
        };
    }

    public ProcessConformanceResponse getProcessConformanceDetail(UUID processInstanceId) {
        ProcessInstance instance = instanceRepository.findById(processInstanceId)
            .orElseThrow(() -> new ResourceNotFoundException("ProcessInstance not found with ID: " + processInstanceId));

        return evaluateConformance(instance);
    }

    public ConformanceAnalyticsSummaryResponse getConformanceAnalyticsSummary(String processType, Instant from, Instant to) {
        List<ProcessInstance> completedInstances = analyticsRepository.findCompletedInstancesForAnalytics(processType, from, to);

        long totalProcesses = completedInstances.size();
        if (totalProcesses == 0) {
            Map<String, Long> emptyTypeCounts = new LinkedHashMap<>();
            emptyTypeCounts.put("MISSING_ACTIVITY", 0L);
            emptyTypeCounts.put("UNEXPECTED_ACTIVITY", 0L);
            emptyTypeCounts.put("ORDER_VIOLATION", 0L);
            emptyTypeCounts.put("TERMINAL_ACTIVITY_VIOLATION", 0L);

            return new ConformanceAnalyticsSummaryResponse(
                processType != null ? processType : "ALL_TYPES",
                0L,
                0L,
                0L,
                0.0,
                0.0,
                0L,
                emptyTypeCounts,
                from,
                to
            );
        }

        long conformantCount = 0;
        long deviatedCount = 0;
        double totalScoreSum = 0.0;
        long totalDeviationCount = 0;

        Map<String, Long> deviationCountsByType = new LinkedHashMap<>();
        deviationCountsByType.put("MISSING_ACTIVITY", 0L);
        deviationCountsByType.put("UNEXPECTED_ACTIVITY", 0L);
        deviationCountsByType.put("ORDER_VIOLATION", 0L);
        deviationCountsByType.put("TERMINAL_ACTIVITY_VIOLATION", 0L);

        for (ProcessInstance instance : completedInstances) {
            ProcessConformanceResponse detail = evaluateConformance(instance);
            totalScoreSum += detail.conformanceScore();
            totalDeviationCount += detail.deviationCount();

            if ("CONFORMANT".equalsIgnoreCase(detail.status())) {
                conformantCount++;
            } else {
                deviatedCount++;
            }

            for (ProcessDeviationDto dev : detail.deviations()) {
                String type = dev.deviationType();
                deviationCountsByType.put(type, deviationCountsByType.getOrDefault(type, 0L) + 1L);
            }
        }

        double conformanceRate = Math.round(((double) conformantCount / totalProcesses * 100.0) * 100.0) / 100.0;
        double avgScore = Math.round((totalScoreSum / totalProcesses) * 100.0) / 100.0;

        return new ConformanceAnalyticsSummaryResponse(
            processType != null ? processType : "ALL_TYPES",
            totalProcesses,
            conformantCount,
            deviatedCount,
            conformanceRate,
            avgScore,
            totalDeviationCount,
            deviationCountsByType,
            from,
            to
        );
    }

    private ProcessConformanceResponse evaluateConformance(ProcessInstance instance) {
        List<String> actualSequence = extractActivitySequence(instance);
        List<String> expectedSequence = getExpectedSequence(instance.getProcessType());

        List<ProcessDeviationDto> deviations = new ArrayList<>();
        List<String> missingActivities = new ArrayList<>();
        List<String> unexpectedActivities = new ArrayList<>();
        List<String> orderViolations = new ArrayList<>();

        String terminalActivity = expectedSequence.isEmpty() ? null : expectedSequence.get(expectedSequence.size() - 1);
        int firstTerminalPos = -1;
        if (terminalActivity != null) {
            for (int i = 0; i < actualSequence.size(); i++) {
                if (terminalActivity.equals(actualSequence.get(i))) {
                    firstTerminalPos = i + 1; // 1-indexed
                    break;
                }
            }
        }

        // 1. Check for Terminal Activity Violations (activities occurring after terminal activity)
        if (firstTerminalPos != -1) {
            for (int i = firstTerminalPos; i < actualSequence.size(); i++) {
                String actName = actualSequence.get(i);
                int actualPos = i + 1;
                deviations.add(new ProcessDeviationDto(
                    "TERMINAL_ACTIVITY_VIOLATION",
                    actName,
                    null,
                    actualPos,
                    "Activity '" + actName + "' occurred at position " + actualPos + " after terminal activity '" + terminalActivity + "' at position " + firstTerminalPos
                ));
            }
        }

        int endLimit = firstTerminalPos != -1 ? firstTerminalPos : actualSequence.size();

        // Evaluate positions across full actualSequence
        Map<String, List<Integer>> positionsByActivity = new HashMap<>();
        for (int i = 0; i < actualSequence.size(); i++) {
            String act = actualSequence.get(i);
            positionsByActivity.computeIfAbsent(act, k -> new ArrayList<>()).add(i + 1);
        }

        // 2. Check MISSING_ACTIVITY
        Set<String> matchedExpectedSet = new HashSet<>();
        for (int i = 0; i < expectedSequence.size(); i++) {
            String expAct = expectedSequence.get(i);
            int expPos = i + 1;
            List<Integer> actualPositions = positionsByActivity.get(expAct);

            if (actualPositions == null || actualPositions.isEmpty()) {
                deviations.add(new ProcessDeviationDto(
                    "MISSING_ACTIVITY",
                    expAct,
                    expPos,
                    null,
                    "Expected activity '" + expAct + "' at position " + expPos + " was not executed"
                ));
                missingActivities.add(expAct);
            } else {
                matchedExpectedSet.add(expAct);
            }
        }

        // 3. Check ORDER_VIOLATION (pre-terminal)
        for (int i = 0; i < expectedSequence.size(); i++) {
            String expEarly = expectedSequence.get(i);
            List<Integer> posEarly = positionsByActivity.get(expEarly);
            if (posEarly == null || posEarly.isEmpty()) continue;

            for (int j = i + 1; j < expectedSequence.size(); j++) {
                String expLater = expectedSequence.get(j);
                List<Integer> posLater = positionsByActivity.get(expLater);
                if (posLater == null || posLater.isEmpty()) continue;

                int firstEarly = posEarly.get(0);
                int firstLater = posLater.get(0);

                if (firstLater < firstEarly) {
                    if (!orderViolations.contains(expEarly)) {
                        deviations.add(new ProcessDeviationDto(
                            "ORDER_VIOLATION",
                            expEarly,
                            i + 1,
                            firstEarly,
                            "Activity '" + expEarly + "' (expected position " + (i + 1) + ") occurred at position " + firstEarly + " after '" + expLater + "' (position " + firstLater + ")"
                        ));
                        orderViolations.add(expEarly);
                    }
                }
            }
        }

        // 4. Check UNEXPECTED_ACTIVITY (pre-terminal extra repetitions or unknown activities)
        for (int i = 0; i < endLimit; i++) {
            String act = actualSequence.get(i);
            int actualPos = i + 1;

            if (!expectedSequence.contains(act)) {
                deviations.add(new ProcessDeviationDto(
                    "UNEXPECTED_ACTIVITY",
                    act,
                    null,
                    actualPos,
                    "Activity '" + act + "' at position " + actualPos + " is not in expected path"
                ));
                if (!unexpectedActivities.contains(act)) {
                    unexpectedActivities.add(act);
                }
            } else {
                List<Integer> posList = positionsByActivity.get(act);
                if (posList.size() > 1 && posList.indexOf(actualPos) > 0) {
                    int expectedPos = expectedSequence.indexOf(act) + 1;
                    deviations.add(new ProcessDeviationDto(
                        "UNEXPECTED_ACTIVITY",
                        act,
                        expectedPos,
                        actualPos,
                        "Unexpected repetition of activity '" + act + "' at position " + actualPos
                    ));
                    if (!unexpectedActivities.contains(act)) {
                        unexpectedActivities.add(act);
                    }
                }
            }
        }

        double rawScore = expectedSequence.isEmpty() ? 1.0 : (double) matchedExpectedSet.size() / expectedSequence.size();
        double conformanceScore = Math.min(1.0, Math.round(rawScore * 100.0) / 100.0);

        String status = deviations.isEmpty() ? "CONFORMANT" : "DEVIATED";

        return new ProcessConformanceResponse(
            instance.getId(),
            instance.getProcessType(),
            expectedSequence,
            actualSequence,
            status,
            conformanceScore,
            deviations.size(),
            deviations,
            missingActivities,
            unexpectedActivities,
            orderViolations
        );
    }

    private List<String> extractActivitySequence(ProcessInstance instance) {
        List<ProcessStep> sortedSteps = instance.getSteps().stream()
            .sorted(Comparator.comparing(ProcessStep::getStartedAt, Comparator.nullsLast(Comparator.naturalOrder())))
            .toList();

        List<String> sequence = sortedSteps.stream()
            .map(ProcessStep::getStepName)
            .filter(Objects::nonNull)
            .toList();

        if (sequence.isEmpty()) {
            List<ProcessEvent> events = eventRepository.findByProcessInstanceIdOrderByTimestampAsc(instance.getId());
            sequence = events.stream()
                .map(ProcessEvent::getEventType)
                .filter(Objects::nonNull)
                .toList();
        }
        return sequence;
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
