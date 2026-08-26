import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { HealthService } from './core/services/health.service';
import { AuthService } from './core/services/auth.service';
import { HealthCheckResponse } from './core/models/health.model';
import { Subscription, interval } from 'rxjs';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './app.component.html',
  styleUrl: './app.component.css'
})
export class AppComponent implements OnInit, OnDestroy {
  title = 'SupplyChainX';
  systemHealth: HealthCheckResponse | null = null;
  healthError: string | null = null;
  currentTime = new Date();

  private healthSub?: Subscription;
  private timerSub?: Subscription;

  constructor(
    public readonly authService: AuthService,
    private readonly healthService: HealthService,
    private readonly router: Router
  ) {}

  ngOnInit(): void {
    this.checkHealth();
    // Poll health every 15 seconds
    this.healthSub = interval(15000).subscribe(() => this.checkHealth());
    // Update live clock every second
    this.timerSub = interval(1000).subscribe(() => {
      this.currentTime = new Date();
    });
  }

  checkHealth(): void {
    this.healthService.getHealth().subscribe({
      next: (res) => {
        this.systemHealth = res;
        this.healthError = null;
      },
      error: (err) => {
        this.systemHealth = null;
        this.healthError = err.message || 'API Unreachable';
      }
    });
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }

  ngOnDestroy(): void {
    this.healthSub?.unsubscribe();
    this.timerSub?.unsubscribe();
  }
}
