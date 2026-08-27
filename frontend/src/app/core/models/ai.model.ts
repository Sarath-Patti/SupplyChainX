export interface ChatMessage {
  role: 'user' | 'assistant';
  content: string;
  toolsInvoked?: string[];
  timestampUtc?: string;
}

export interface ChatRequest {
  message: string;
  history?: ChatMessage[];
}

export interface ChatResponse {
  response: string;
  toolsInvoked?: string[];
  timestampUtc: string;
}
