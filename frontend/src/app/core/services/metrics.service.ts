import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { MetricsResponse } from '../models/metrics.model';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class MetricsService {
  private readonly baseUrl = environment.apiBaseUrl;

  constructor(private readonly http: HttpClient) {}

  getMetrics(): Observable<MetricsResponse> {
    return this.http.get<MetricsResponse>(`${this.baseUrl}/api/v1/metrics`);
  }
}
