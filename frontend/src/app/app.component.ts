import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { SystemStatusService, SystemInformation } from './core/services/system-status.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './app.component.html',
  styleUrl: './app.component.css'
})
export class AppComponent implements OnInit {
  systemInfo!: SystemInformation;

  constructor(private readonly systemStatusService: SystemStatusService) {}

  ngOnInit(): void {
    this.systemInfo = this.systemStatusService.getSystemInformation();
  }
}
