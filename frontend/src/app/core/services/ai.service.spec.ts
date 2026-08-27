import { TestBed } from '@angular/core';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { AiService } from './ai.service';
import { ChatResponse } from '../models/ai.model';
import { environment } from '../../../environments/environment';

describe('AiService', () => {
  let service: AiService;
  let httpMock: HttpTestingController;

  const mockChatResponse: ChatResponse = {
    response: 'Low stock items retrieved.',
    toolsInvoked: ['GetLowStockItemsAsync'],
    timestampUtc: new Date().toISOString()
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [AiService]
    });

    service = TestBed.inject(AiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should send POST request to /api/v1/ai/chat with message and history', () => {
    const prompt = 'Which products are low in stock?';
    service.chat(prompt).subscribe((res) => {
      expect(res.response).toBe('Low stock items retrieved.');
      expect(res.toolsInvoked).toContain('GetLowStockItemsAsync');
    });

    const req = httpMock.expectOne(`${environment.apiBaseUrl}/api/v1/ai/chat`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body.message).toBe(prompt);
    req.flush(mockChatResponse);
  });
});
