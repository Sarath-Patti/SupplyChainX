package com.supplychainx.processservice.repository;

import com.supplychainx.processservice.entity.ProcessEvent;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.stereotype.Repository;

import java.util.List;
import java.util.Optional;
import java.util.UUID;

@Repository
public interface ProcessEventRepository extends JpaRepository<ProcessEvent, UUID> {

    List<ProcessEvent> findByProcessInstanceIdOrderByTimestampAsc(UUID processInstanceId);

    boolean existsByEventId(UUID eventId);

    Optional<ProcessEvent> findByEventId(UUID eventId);
}
