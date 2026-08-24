export interface HealthCheckItem {
  name: string;
  status: string;
  description: string;
}

export interface HealthCheckResponse {
  status: string;
  service: string;
  version?: string;
  timestamp: string;
  checks?: HealthCheckItem[];
}
