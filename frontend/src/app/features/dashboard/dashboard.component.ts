import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HealthService } from '../../core/services/health.service';
import { MetricsService } from '../../core/services/metrics.service';
import { HealthCheckResponse } from '../../core/models/health.model';
import { MetricsResponse } from '../../core/models/metrics.model';
import { Subscription, interval, forkJoin } from 'rxjs';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.css'
})
export class DashboardComponent implements OnInit, OnDestroy {
  healthData: HealthCheckResponse | null = null;
  readinessData: HealthCheckResponse | null = null;
  livenessData: HealthCheckResponse | null = null;
  metricsData: MetricsResponse | null = null;

  isLoading = true;
  errorMessage: string | null = null;
  autoRefresh = true;
  lastUpdated: Date = new Date();

  private pollSub?: Subscription;

  constructor(
    private readonly healthService: HealthService,
    private readonly metricsService: MetricsService
  ) {}

  ngOnInit(): void {
    this.loadData();
    this.pollSub = interval(5000).subscribe(() => {
      if (this.autoRefresh) {
        this.loadData(false);
      }
    });
  }

  loadData(showLoading = true): void {
    if (showLoading) {
      this.isLoading = true;
    }
    this.errorMessage = null;

    forkJoin({
      health: this.healthService.getHealth(),
      readiness: this.healthService.getReadiness(),
      liveness: this.healthService.getLiveness(),
      metrics: this.metricsService.getMetrics()
    }).subscribe({
      next: (res) => {
        this.healthData = res.health;
        this.readinessData = res.readiness;
        this.livenessData = res.liveness;
        this.metricsData = res.metrics;
        this.isLoading = false;
        this.lastUpdated = new Date();
      },
      error: (err) => {
        this.errorMessage = err.message || 'Failed to connect to backend server. Make sure API is running on localhost:5000';
        this.isLoading = false;
      }
    });
  }

  toggleAutoRefresh(): void {
    this.autoRefresh = !this.autoRefresh;
  }

  formatBytes(bytes: number): string {
    if (!bytes) return '0 MB';
    const mb = bytes / (1024 * 1024);
    return `${mb.toFixed(2)} MB`;
  }

  formatUptime(seconds: number): string {
    if (!seconds && seconds !== 0) return '0s';
    const hrs = Math.floor(seconds / 3600);
    const mins = Math.floor((seconds % 3600) / 60);
    const secs = seconds % 60;
    if (hrs > 0) return `${hrs}h ${mins}m ${secs}s`;
    if (mins > 0) return `${mins}m ${secs}s`;
    return `${secs}s`;
  }

  ngOnDestroy(): void {
    this.pollSub?.unsubscribe();
  }
}
