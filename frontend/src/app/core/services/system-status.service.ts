import { Injectable } from '@angular/core';

export interface SystemInformation {
  name: string;
  version: string;
  milestone: string;
  environment: string;
  status: string;
  techStack: string[];
}

@Injectable({
  providedIn: 'root'
})
export class SystemStatusService {
  getSystemInformation(): SystemInformation {
    return {
      name: 'SupplyChainX',
      version: 'v0.1.0',
      milestone: 'Milestone v0.1 – Project Foundation',
      environment: 'Development',
      status: 'Platform Foundation Initialized',
      techStack: [
        'Angular + TypeScript',
        'C# + ASP.NET Core Web API',
        'PostgreSQL 16',
        'Entity Framework Core',
        'Apache Kafka (KRaft)',
        'xUnit & Docker'
      ]
    };
  }
}
