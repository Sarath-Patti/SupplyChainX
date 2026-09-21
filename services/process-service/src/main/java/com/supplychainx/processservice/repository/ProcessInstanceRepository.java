package com.supplychainx.processservice.repository;

import com.supplychainx.processservice.entity.ProcessInstance;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.stereotype.Repository;

import java.util.List;
import java.util.Optional;
import java.util.UUID;

@Repository
public interface ProcessInstanceRepository extends JpaRepository<ProcessInstance, UUID> {

    Optional<ProcessInstance> findByBusinessKey(String businessKey);

    boolean existsByBusinessKey(String businessKey);

    List<ProcessInstance> findByStatus(String status);

    List<ProcessInstance> findByProcessType(String processType);
}
