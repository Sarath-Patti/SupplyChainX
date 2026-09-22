package com.supplychainx.processservice.service;

import com.supplychainx.processservice.dto.*;
import com.supplychainx.processservice.entity.ProcessEvent;
import com.supplychainx.processservice.entity.ProcessInstance;
import com.supplychainx.processservice.entity.ProcessStep;
import com.supplychainx.processservice.exception.ResourceNotFoundException;
import com.supplychainx.processservice.repository.ProcessEventRepository;
import com.supplychainx.processservice.repository.ProcessInstanceRepository;
import com.supplychainx.processservice.repository.ProcessStepRepository;
import com.supplychainx.processservice.kafka.KafkaEventMapper;
import com.supplychainx.processservice.kafka.model.SupplyChainXDomainEventDto;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.dao.DataIntegrityViolationException;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.time.Instant;
import java.util.List;
import java.util.UUID;

@Service
@Transactional(readOnly = true)
public class ProcessService {

    private static final Logger log = LoggerFactory.getLogger(ProcessService.class);

    private final ProcessInstanceRepository instanceRepository;
    private final ProcessEventRepository eventRepository;
    private final ProcessStepRepository stepRepository;
    private final KafkaEventMapper kafkaEventMapper;

    public ProcessService(
        ProcessInstanceRepository instanceRepository,
        ProcessEventRepository eventRepository,
        ProcessStepRepository stepRepository,
        KafkaEventMapper kafkaEventMapper) {
        this.instanceRepository = instanceRepository;
        this.eventRepository = eventRepository;
        this.stepRepository = stepRepository;
        this.kafkaEventMapper = kafkaEventMapper;
    }

    public List<ProcessInstanceResponseDto> getAllProcesses() {
        return instanceRepository.findAll().stream()
            .map(this::mapToResponseDto)
            .toList();
    }

    public ProcessInstanceDetailResponseDto getProcessById(UUID id) {
        ProcessInstance instance = instanceRepository.findById(id)
            .orElseThrow(() -> new ResourceNotFoundException("ProcessInstance not found with ID: " + id));
        return mapToDetailResponseDto(instance);
    }

    public List<ProcessEventResponseDto> getEventsForProcess(UUID processInstanceId) {
        if (!instanceRepository.existsById(processInstanceId)) {
            throw new ResourceNotFoundException("ProcessInstance not found with ID: " + processInstanceId);
        }
        return eventRepository.findByProcessInstanceIdOrderByTimestampAsc(processInstanceId).stream()
            .map(this::mapToEventResponseDto)
            .toList();
    }

    @Transactional
    public ProcessInstanceResponseDto createProcess(CreateProcessInstanceRequestDto request) {
        if (instanceRepository.existsByBusinessKey(request.businessKey())) {
            throw new IllegalArgumentException("ProcessInstance with businessKey '" + request.businessKey() + "' already exists.");
        }

        ProcessInstance instance = new ProcessInstance(
            request.businessKey(),
            request.processType(),
            request.status(),
            request.startedAt()
        );

        ProcessInstance saved = instanceRepository.save(instance);
        return mapToResponseDto(saved);
    }

    @Transactional
    public ProcessEventResponseDto addEventToProcess(UUID processInstanceId, UUID eventId, String eventType, Instant timestamp, String payload) {
        ProcessInstance instance = instanceRepository.findById(processInstanceId)
            .orElseThrow(() -> new ResourceNotFoundException("ProcessInstance not found with ID: " + processInstanceId));

        if (eventRepository.existsByEventId(eventId)) {
            throw new IllegalArgumentException("ProcessEvent with eventId '" + eventId + "' already exists.");
        }

        ProcessEvent event = new ProcessEvent(eventId, eventType, timestamp, payload);
        instance.addEvent(event);

        ProcessEvent saved = eventRepository.save(event);
        return mapToEventResponseDto(saved);
    }

    @Transactional
    public ProcessEventResponseDto processDomainEvent(SupplyChainXDomainEventDto eventDto, String rawJson) {
        if (eventDto.eventId() == null) {
            throw new IllegalArgumentException("Event is missing required eventId.");
        }

        if (eventRepository.existsByEventId(eventDto.eventId())) {
            log.info("ProcessEvent with eventId {} already processed. Skipping duplicate.", eventDto.eventId());
            return eventRepository.findByEventId(eventDto.eventId())
                .map(this::mapToEventResponseDto)
                .orElse(null);
        }

        String businessKey = kafkaEventMapper.extractBusinessKey(eventDto)
            .orElseThrow(() -> new IllegalArgumentException("Event is missing a valid business identifier (productId, warehouseId, or inventoryId)."));

        String processType = kafkaEventMapper.determineProcessType(eventDto);
        Instant eventTimestamp = eventDto.occurredOnUtc() != null ? eventDto.occurredOnUtc() : Instant.now();

        ProcessInstance instance = instanceRepository.findByBusinessKey(businessKey)
            .orElseGet(() -> {
                ProcessInstance newInstance = new ProcessInstance(
                    businessKey,
                    processType,
                    "ACTIVE",
                    eventTimestamp
                );
                return instanceRepository.save(newInstance);
            });

        if (kafkaEventMapper.isTerminalEvent(eventDto)) {
            instance.setStatus("COMPLETED");
            instance.setCompletedAt(eventTimestamp);
        }

        ProcessEvent event = new ProcessEvent(
            eventDto.eventId(),
            eventDto.eventType() != null ? eventDto.eventType() : "UNKNOWN_EVENT",
            eventTimestamp,
            rawJson
        );
        instance.addEvent(event);

        kafkaEventMapper.determineStepName(eventDto).ifPresent(stepName -> {
            List<ProcessStep> existingSteps = instance.getSteps();
            if (!existingSteps.isEmpty()) {
                ProcessStep lastStep = existingSteps.get(existingSteps.size() - 1);
                if (lastStep.getCompletedAt() == null || lastStep.getCompletedAt().equals(lastStep.getStartedAt())) {
                    lastStep.setCompletedAt(eventTimestamp);
                }
            }

            ProcessStep step = new ProcessStep(
                stepName,
                "COMPLETED",
                eventTimestamp,
                eventTimestamp
            );
            instance.addStep(step);
        });

        try {
            ProcessEvent saved = eventRepository.save(event);
            instanceRepository.save(instance);
            return mapToEventResponseDto(saved);
        } catch (DataIntegrityViolationException e) {
            log.warn("Duplicate ProcessEvent detected via database unique constraint for eventId: {}", eventDto.eventId());
            return eventRepository.findByEventId(eventDto.eventId())
                .map(this::mapToEventResponseDto)
                .orElse(null);
        }
    }

    private ProcessInstanceResponseDto mapToResponseDto(ProcessInstance instance) {
        return new ProcessInstanceResponseDto(
            instance.getId(),
            instance.getBusinessKey(),
            instance.getProcessType(),
            instance.getStatus(),
            instance.getStartedAt(),
            instance.getCompletedAt(),
            instance.getCreatedAt(),
            instance.getUpdatedAt()
        );
    }

    private ProcessInstanceDetailResponseDto mapToDetailResponseDto(ProcessInstance instance) {
        List<ProcessStepResponseDto> steps = instance.getSteps().stream()
            .map(this::mapToStepResponseDto)
            .toList();

        List<ProcessEventResponseDto> events = eventRepository
            .findByProcessInstanceIdOrderByTimestampAsc(instance.getId()).stream()
            .map(this::mapToEventResponseDto)
            .toList();

        return new ProcessInstanceDetailResponseDto(
            instance.getId(),
            instance.getBusinessKey(),
            instance.getProcessType(),
            instance.getStatus(),
            instance.getStartedAt(),
            instance.getCompletedAt(),
            instance.getCreatedAt(),
            instance.getUpdatedAt(),
            steps,
            events
        );
    }

    private ProcessEventResponseDto mapToEventResponseDto(ProcessEvent event) {
        return new ProcessEventResponseDto(
            event.getId(),
            event.getEventId(),
            event.getProcessInstance().getId(),
            event.getEventType(),
            event.getTimestamp(),
            event.getPayload(),
            event.getCreatedAt()
        );
    }

    private ProcessStepResponseDto mapToStepResponseDto(ProcessStep step) {
        return new ProcessStepResponseDto(
            step.getId(),
            step.getProcessInstance().getId(),
            step.getStepName(),
            step.getStatus(),
            step.getStartedAt(),
            step.getCompletedAt(),
            step.getCreatedAt()
        );
    }
}
