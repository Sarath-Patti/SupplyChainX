import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { HealthCheckResponse } from '../models/health.model';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class HealthService {
  private readonly baseUrl = environment.apiBaseUrl;

  constructor(private readonly http: HttpClient) {}

  getHealth(): Observable<HealthCheckResponse> {
    return this.http.get<HealthCheckResponse>(`${this.baseUrl}/health`);
  }

  getReadiness(): Observable<HealthCheckResponse> {
    return this.http.get<HealthCheckResponse>(`${this.baseUrl}/health/ready`);
  }

  getLiveness(): Observable<HealthCheckResponse> {
    return this.http.get<HealthCheckResponse>(`${this.baseUrl}/health/live`);
  }
}
