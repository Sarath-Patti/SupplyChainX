package com.supplychainx.processservice.controller;

import com.supplychainx.processservice.dto.*;
import com.supplychainx.processservice.service.ProcessService;
import jakarta.validation.Valid;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;

import java.util.List;
import java.util.UUID;

@RestController
@RequestMapping("/api/v1/processes")
public class ProcessController {

    private final ProcessService processService;

    public ProcessController(ProcessService processService) {
        this.processService = processService;
    }

    @GetMapping
    public ResponseEntity<List<ProcessInstanceResponseDto>> getAllProcesses() {
        List<ProcessInstanceResponseDto> processes = processService.getAllProcesses();
        return ResponseEntity.ok(processes);
    }

    @GetMapping("/{id}")
    public ResponseEntity<ProcessInstanceDetailResponseDto> getProcessById(@PathVariable UUID id) {
        ProcessInstanceDetailResponseDto process = processService.getProcessById(id);
        return ResponseEntity.ok(process);
    }

    @GetMapping("/{id}/events")
    public ResponseEntity<List<ProcessEventResponseDto>> getEventsForProcess(@PathVariable UUID id) {
        List<ProcessEventResponseDto> events = processService.getEventsForProcess(id);
        return ResponseEntity.ok(events);
    }

    @PostMapping
    public ResponseEntity<ProcessInstanceResponseDto> createProcess(@Valid @RequestBody CreateProcessInstanceRequestDto request) {
        ProcessInstanceResponseDto created = processService.createProcess(request);
        return new ResponseEntity<>(created, HttpStatus.CREATED);
    }
}
