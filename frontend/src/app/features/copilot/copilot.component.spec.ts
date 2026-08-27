import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { of } from 'rxjs';
import { CopilotComponent } from './copilot.component';
import { AiService } from '../../core/services/ai.service';
import { ChatResponse } from '../../core/models/ai.model';

describe('CopilotComponent', () => {
  let component: CopilotComponent;
  let fixture: ComponentFixture<CopilotComponent>;
  let aiServiceSpy: jasmine.SpyObj<AiService>;

  const mockResponse: ChatResponse = {
    response: 'Here are the active warehouses.',
    toolsInvoked: ['GetWarehousesAsync'],
    timestampUtc: new Date().toISOString()
  };

  beforeEach(async () => {
    aiServiceSpy = jasmine.createSpyObj('AiService', ['chat']);
    aiServiceSpy.chat.and.returnValue(of(mockResponse));

    await TestBed.configureTestingModule({
      imports: [CopilotComponent, HttpClientTestingModule],
      providers: [{ provide: AiService, useValue: aiServiceSpy }]
    }).compileComponents();

    fixture = TestBed.createComponent(CopilotComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should send user prompt and append assistant response to message thread', () => {
    component.userPrompt = 'List active warehouses';
    component.sendMessage();

    expect(aiServiceSpy.chat).toHaveBeenCalledWith('List active warehouses', jasmine.any(Array));
    expect(component.messages.length).toBe(3); // Initial welcome + User + Assistant
    expect(component.messages[2].content).toBe('Here are the active warehouses.');
    expect(component.messages[2].toolsInvoked).toContain('GetWarehousesAsync');
  });

  it('should reset session when clearChat is clicked', () => {
    component.messages.push({ role: 'user', content: 'Test' });
    expect(component.messages.length).toBe(2);

    component.clearChat();
    expect(component.messages.length).toBe(1);
    expect(component.messages[0].content).toContain('session reset');
  });
});
