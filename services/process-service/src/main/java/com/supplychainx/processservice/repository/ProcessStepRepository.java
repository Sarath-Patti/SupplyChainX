package com.supplychainx.processservice.repository;

import com.supplychainx.processservice.entity.ProcessStep;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.stereotype.Repository;

import java.util.List;
import java.util.UUID;

@Repository
public interface ProcessStepRepository extends JpaRepository<ProcessStep, UUID> {

    List<ProcessStep> findByProcessInstanceId(UUID processInstanceId);

    List<ProcessStep> findByProcessInstanceIdOrderByStartedAtAsc(UUID processInstanceId);
}
