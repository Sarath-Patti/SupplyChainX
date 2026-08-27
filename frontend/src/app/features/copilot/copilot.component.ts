import { Component, ElementRef, ViewChild, AfterViewChecked } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { AiService } from '../../core/services/ai.service';
import { ChatMessage } from '../../core/models/ai.model';

@Component({
  selector: 'app-copilot',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './copilot.component.html',
  styleUrls: ['./copilot.component.css']
})
export class CopilotComponent implements AfterViewChecked {
  @ViewChild('chatContainer') private chatContainer!: ElementRef;

  userPrompt = '';
  isLoading = false;
  errorMessage: string | null = null;

  messages: ChatMessage[] = [
    {
      role: 'assistant',
      content: '### 🤖 Welcome to SupplyChainX Agentic AI Copilot!\n\nI am powered by Microsoft Semantic Kernel & Model Context Protocol (MCP). I can automatically plan and execute multi-tool workflows across products, warehouses, and inventory telemetry.',
      timestampUtc: new Date().toISOString()
    }
  ];

  suggestedPrompts = [
    { label: '⚠️ Low Stock Alerts', query: 'Which products are low in stock?' },
    { label: '📦 Product Catalog', query: 'List products in the catalog' },
    { label: '🏭 Warehouse Status', query: 'List active warehouses' },
    { label: '📋 Inventory Summary', query: 'Give me an inventory summary' }
  ];

  constructor(private readonly aiService: AiService) {}

  ngAfterViewChecked(): void {
    this.scrollToBottom();
  }

  sendMessage(promptOverride?: string): void {
    const textToSend = (promptOverride || this.userPrompt).trim();
    if (!textToSend || this.isLoading) return;

    this.userPrompt = '';
    this.errorMessage = null;

    const userMessage: ChatMessage = {
      role: 'user',
      content: textToSend,
      timestampUtc: new Date().toISOString()
    };

    this.messages.push(userMessage);
    this.isLoading = true;

    this.aiService.chat(textToSend, this.messages.slice(0, -1)).subscribe({
      next: (res) => {
        const assistantMessage: ChatMessage = {
          role: 'assistant',
          content: res.response,
          toolsInvoked: res.toolsInvoked,
          activityTrace: res.activityTrace,
          timestampUtc: res.timestampUtc
        };
        this.messages.push(assistantMessage);
        this.isLoading = false;
      },
      error: (err) => {
        this.isLoading = false;
        this.errorMessage = err.error?.error || err.error?.message || 'Failed to connect to AI Copilot service.';
      }
    });
  }

  clearChat(): void {
    this.messages = [
      {
        role: 'assistant',
        content: 'Agentic session reset. How can I assist you with SupplyChainX operations?',
        timestampUtc: new Date().toISOString()
      }
    ];
    this.errorMessage = null;
  }

  private scrollToBottom(): void {
    try {
      if (this.chatContainer) {
        this.chatContainer.nativeElement.scrollTop = this.chatContainer.nativeElement.scrollHeight;
      }
    } catch { }
  }
}
