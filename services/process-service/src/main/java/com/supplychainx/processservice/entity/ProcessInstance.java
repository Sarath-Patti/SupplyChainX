package com.supplychainx.processservice.entity;

import jakarta.persistence.*;
import java.time.Instant;
import java.util.ArrayList;
import java.util.List;
import java.util.Objects;
import java.util.UUID;

@Entity
@Table(
    name = "process_instances",
    indexes = {
        @Index(name = "idx_process_instance_business_key", columnList = "business_key", unique = true),
        @Index(name = "idx_process_instance_status", columnList = "status"),
        @Index(name = "idx_process_instance_process_type", columnList = "process_type"),
        @Index(name = "idx_process_instance_started_at", columnList = "started_at"),
        @Index(name = "idx_process_instance_completed_at", columnList = "completed_at"),
        @Index(name = "idx_process_instance_type_started", columnList = "process_type, started_at")
    }
)
public class ProcessInstance {

    @Id
    @GeneratedValue(strategy = GenerationType.UUID)
    private UUID id;

    @Column(name = "business_key", nullable = false, unique = true, length = 128)
    private String businessKey;

    @Column(name = "process_type", nullable = false, length = 64)
    private String processType;

    @Column(name = "status", nullable = false, length = 32)
    private String status;

    @Column(name = "started_at", nullable = false)
    private Instant startedAt;

    @Column(name = "completed_at")
    private Instant completedAt;

    @Column(name = "created_at", nullable = false, updatable = false)
    private Instant createdAt;

    @Column(name = "updated_at")
    private Instant updatedAt;

    @OneToMany(mappedBy = "processInstance", cascade = CascadeType.ALL, orphanRemoval = true, fetch = FetchType.LAZY)
    private List<ProcessEvent> events = new ArrayList<>();

    @OneToMany(mappedBy = "processInstance", cascade = CascadeType.ALL, orphanRemoval = true, fetch = FetchType.LAZY)
    private List<ProcessStep> steps = new ArrayList<>();

    public ProcessInstance() {
    }

    public ProcessInstance(String businessKey, String processType, String status, Instant startedAt) {
        this.businessKey = businessKey;
        this.processType = processType;
        this.status = status;
        this.startedAt = startedAt != null ? startedAt : Instant.now();
        this.createdAt = Instant.now();
        this.updatedAt = Instant.now();
    }

    @PrePersist
    protected void onCreate() {
        if (createdAt == null) {
            createdAt = Instant.now();
        }
        if (startedAt == null) {
            startedAt = createdAt;
        }
        updatedAt = Instant.now();
    }

    @PreUpdate
    protected void onUpdate() {
        updatedAt = Instant.now();
    }

    public void addEvent(ProcessEvent event) {
        events.add(event);
        event.setProcessInstance(this);
    }

    public void addStep(ProcessStep step) {
        steps.add(step);
        step.setProcessInstance(this);
    }

    // Getters and Setters

    public UUID getId() {
        return id;
    }

    public void setId(UUID id) {
        this.id = id;
    }

    public String getBusinessKey() {
        return businessKey;
    }

    public void setBusinessKey(String businessKey) {
        this.businessKey = businessKey;
    }

    public String getProcessType() {
        return processType;
    }

    public void setProcessType(String processType) {
        this.processType = processType;
    }

    public String getStatus() {
        return status;
    }

    public void setStatus(String status) {
        this.status = status;
    }

    public Instant getStartedAt() {
        return startedAt;
    }

    public void setStartedAt(Instant startedAt) {
        this.startedAt = startedAt;
    }

    public Instant getCompletedAt() {
        return completedAt;
    }

    public void setCompletedAt(Instant completedAt) {
        this.completedAt = completedAt;
    }

    public Instant getCreatedAt() {
        return createdAt;
    }

    public void setCreatedAt(Instant createdAt) {
        this.createdAt = createdAt;
    }

    public Instant getUpdatedAt() {
        return updatedAt;
    }

    public void setUpdatedAt(Instant updatedAt) {
        this.updatedAt = updatedAt;
    }

    public List<ProcessEvent> getEvents() {
        return events;
    }

    public void setEvents(List<ProcessEvent> events) {
        this.events = events;
    }

    public List<ProcessStep> getSteps() {
        return steps;
    }

    public void setSteps(List<ProcessStep> steps) {
        this.steps = steps;
    }

    @Override
    public boolean equals(Object o) {
        if (this == o) return true;
        if (o == null || getClass() != o.getClass()) return false;
        ProcessInstance that = (ProcessInstance) o;
        return Objects.equals(id, that.id);
    }

    @Override
    public int hashCode() {
        return Objects.hash(id);
    }
}
