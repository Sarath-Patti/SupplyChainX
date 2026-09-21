package com.supplychainx.processservice.service;

import com.supplychainx.processservice.dto.*;
import com.supplychainx.processservice.entity.ProcessEvent;
import com.supplychainx.processservice.entity.ProcessInstance;
import com.supplychainx.processservice.entity.ProcessStep;
import com.supplychainx.processservice.exception.ResourceNotFoundException;
import com.supplychainx.processservice.repository.ProcessEventRepository;
import com.supplychainx.processservice.repository.ProcessInstanceRepository;
import com.supplychainx.processservice.repository.ProcessStepRepository;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.time.Instant;
import java.util.List;
import java.util.UUID;

@Service
@Transactional(readOnly = true)
public class ProcessService {

    private final ProcessInstanceRepository instanceRepository;
    private final ProcessEventRepository eventRepository;
    private final ProcessStepRepository stepRepository;

    public ProcessService(
        ProcessInstanceRepository instanceRepository,
        ProcessEventRepository eventRepository,
        ProcessStepRepository stepRepository) {
        this.instanceRepository = instanceRepository;
        this.eventRepository = eventRepository;
        this.stepRepository = stepRepository;
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
