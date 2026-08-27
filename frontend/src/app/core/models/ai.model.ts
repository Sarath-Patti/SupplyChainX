export interface AgentActivityStep {
  step: string;
  toolName: string;
  status: string;
  details: string;
}

export interface ChatMessage {
  role: 'user' | 'assistant';
  content: string;
  toolsInvoked?: string[];
  activityTrace?: AgentActivityStep[];
  timestampUtc?: string;
}

export interface ChatRequest {
  message: string;
  history?: ChatMessage[];
}

export interface ChatResponse {
  response: string;
  toolsInvoked?: string[];
  activityTrace?: AgentActivityStep[];
  timestampUtc: string;
}
