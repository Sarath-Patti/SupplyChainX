export interface KafkaConsumerStatus {
  isRunning: boolean;
  consumerGroupId: string;
  subscribedTopics: string[];
  lastEventConsumedAtUtc?: string | null;
  lastEventProcessedAtUtc?: string | null;
  lastProcessingFailureAtUtc?: string | null;
  lastProcessingFailureReason?: string | null;
}

export interface KafkaMetrics {
  eventsConsumed: number;
  eventsProcessed: number;
  duplicateEventsSkipped: number;
  processingFailures: number;
  retryAttempts: number;
  eventsPublishedToDlq: number;
  malformedEvents: number;
  dlqSuccessCount: number;
  dlqFailureCount: number;
}

export interface SystemMetrics {
  uptimeSeconds: number;
  processId: number;
  workingSetBytes: number;
  threadCount: number;
}

export interface MetricsResponse {
  timestamp: string;
  service: string;
  version: string;
  consumerStatus: KafkaConsumerStatus;
  metrics: KafkaMetrics;
  system: SystemMetrics;
}
