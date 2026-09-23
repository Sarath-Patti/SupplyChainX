package com.supplychainx.processservice.controller;

import com.fasterxml.jackson.databind.ObjectMapper;
import com.supplychainx.processservice.dto.CreateProcessInstanceRequestDto;
import com.supplychainx.processservice.dto.ProcessEventResponseDto;
import com.supplychainx.processservice.dto.ProcessInstanceDetailResponseDto;
import com.supplychainx.processservice.dto.ProcessInstanceResponseDto;
import com.supplychainx.processservice.exception.ResourceNotFoundException;
import com.supplychainx.processservice.service.ProcessService;
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
import static org.mockito.Mockito.when;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.get;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.post;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.*;

import org.springframework.security.test.context.support.WithMockUser;
import org.springframework.test.context.ActiveProfiles;
import org.springframework.test.web.servlet.MockMvc;

import java.time.Instant;
import java.util.Collections;
import java.util.List;
import java.util.UUID;

import static org.mockito.ArgumentMatchers.any;
import static org.mockito.Mockito.when;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.get;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.post;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.*;

@WebMvcTest(ProcessController.class)
@ActiveProfiles("test")
@WithMockUser(roles = "ANALYST")
class ProcessControllerTests {

    @Autowired
    private MockMvc mockMvc;

    @Autowired
    private ObjectMapper objectMapper;

    @MockBean
    private ProcessService processService;

    @MockBean
    private com.supplychainx.processservice.security.JwtTokenProvider jwtTokenProvider;

    @Test
    void shouldReturnAllProcesses() throws Exception {
        UUID processId = UUID.randomUUID();
        ProcessInstanceResponseDto dto = new ProcessInstanceResponseDto(
            processId,
            "BIZ-KEY-001",
            "ORDER_FULFILLMENT",
            "RUNNING",
            Instant.now(),
            null,
            Instant.now(),
            Instant.now()
        );

        when(processService.getAllProcesses()).thenReturn(List.of(dto));

        mockMvc.perform(get("/api/v1/processes"))
            .andExpect(status().isOk())
            .andExpect(content().contentType(MediaType.APPLICATION_JSON))
            .andExpect(jsonPath("$[0].id").value(processId.toString()))
            .andExpect(jsonPath("$[0].businessKey").value("BIZ-KEY-001"))
            .andExpect(jsonPath("$[0].processType").value("ORDER_FULFILLMENT"));
    }

    @Test
    void shouldReturnProcessByIdWhenExists() throws Exception {
        UUID processId = UUID.randomUUID();
        ProcessInstanceDetailResponseDto dto = new ProcessInstanceDetailResponseDto(
            processId,
            "BIZ-KEY-002",
            "INVENTORY_ADJUSTMENT",
            "COMPLETED",
            Instant.now(),
            Instant.now(),
            Instant.now(),
            Instant.now(),
            Collections.emptyList(),
            Collections.emptyList()
        );

        when(processService.getProcessById(processId)).thenReturn(dto);

        mockMvc.perform(get("/api/v1/processes/{id}", processId))
            .andExpect(status().isOk())
            .andExpect(jsonPath("$.id").value(processId.toString()))
            .andExpect(jsonPath("$.businessKey").value("BIZ-KEY-002"))
            .andExpect(jsonPath("$.status").value("COMPLETED"));
    }

    @Test
    void shouldReturn404WhenProcessNotFound() throws Exception {
        UUID processId = UUID.randomUUID();
        when(processService.getProcessById(processId))
            .thenThrow(new ResourceNotFoundException("ProcessInstance not found with ID: " + processId));

        mockMvc.perform(get("/api/v1/processes/{id}", processId))
            .andExpect(status().isNotFound())
            .andExpect(jsonPath("$.status").value(404))
            .andExpect(jsonPath("$.error").value("Not Found"));
    }

    @Test
    void shouldReturnEventsForProcess() throws Exception {
        UUID processId = UUID.randomUUID();
        UUID eventId = UUID.randomUUID();

        ProcessEventResponseDto eventDto = new ProcessEventResponseDto(
            UUID.randomUUID(),
            eventId,
            processId,
            "ProductCreatedEvent",
            Instant.now(),
            "{\"sku\": \"SKU-001\"}",
            Instant.now()
        );

        when(processService.getEventsForProcess(processId)).thenReturn(List.of(eventDto));

        mockMvc.perform(get("/api/v1/processes/{id}/events", processId))
            .andExpect(status().isOk())
            .andExpect(jsonPath("$[0].eventId").value(eventId.toString()))
            .andExpect(jsonPath("$[0].eventType").value("ProductCreatedEvent"));
    }

    @Test
    void shouldCreateProcessInstance() throws Exception {
        UUID processId = UUID.randomUUID();
        CreateProcessInstanceRequestDto request = new CreateProcessInstanceRequestDto(
            "BIZ-NEW-100",
            "ORDER_FULFILLMENT",
            "RUNNING",
            Instant.now()
        );

        ProcessInstanceResponseDto response = new ProcessInstanceResponseDto(
            processId,
            "BIZ-NEW-100",
            "ORDER_FULFILLMENT",
            "RUNNING",
            Instant.now(),
            null,
            Instant.now(),
            Instant.now()
        );

        when(processService.createProcess(any(CreateProcessInstanceRequestDto.class))).thenReturn(response);

        mockMvc.perform(post("/api/v1/processes")
                .with(org.springframework.security.test.web.servlet.request.SecurityMockMvcRequestPostProcessors.csrf())
                .contentType(MediaType.APPLICATION_JSON)
                .content(objectMapper.writeValueAsString(request)))
            .andExpect(status().isCreated())
            .andExpect(jsonPath("$.id").value(processId.toString()))
            .andExpect(jsonPath("$.businessKey").value("BIZ-NEW-100"));
    }
}
