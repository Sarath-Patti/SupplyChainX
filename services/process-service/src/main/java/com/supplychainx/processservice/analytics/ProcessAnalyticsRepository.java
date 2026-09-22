package com.supplychainx.processservice.analytics;

import com.supplychainx.processservice.entity.ProcessInstance;
import org.springframework.data.jpa.domain.Specification;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.JpaSpecificationExecutor;
import org.springframework.stereotype.Repository;

import java.time.Instant;
import java.util.List;
import java.util.UUID;

@Repository
public interface ProcessAnalyticsRepository extends JpaRepository<ProcessInstance, UUID>, JpaSpecificationExecutor<ProcessInstance> {

    default List<ProcessInstance> findInstancesForAnalytics(String processType, Instant from, Instant to) {
        Specification<ProcessInstance> spec = (root, query, cb) -> cb.conjunction();

        if (processType != null && !processType.isBlank()) {
            spec = spec.and((root, query, cb) -> cb.equal(root.get("processType"), processType));
        }
        if (from != null) {
            spec = spec.and((root, query, cb) -> cb.greaterThanOrEqualTo(root.get("startedAt"), from));
        }
        if (to != null) {
            spec = spec.and((root, query, cb) -> cb.lessThanOrEqualTo(root.get("startedAt"), to));
        }

        return findAll(spec);
    }

    default List<ProcessInstance> findCompletedInstancesForAnalytics(String processType, Instant from, Instant to) {
        Specification<ProcessInstance> spec = (root, query, cb) -> cb.equal(root.get("status"), "COMPLETED");

        if (processType != null && !processType.isBlank()) {
            spec = spec.and((root, query, cb) -> cb.equal(root.get("processType"), processType));
        }
        if (from != null) {
            spec = spec.and((root, query, cb) -> cb.greaterThanOrEqualTo(root.get("completedAt"), from));
        }
        if (to != null) {
            spec = spec.and((root, query, cb) -> cb.lessThanOrEqualTo(root.get("completedAt"), to));
        }

        return findAll(spec);
    }
}
