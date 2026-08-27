import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ChatMessage, ChatResponse } from '../models/ai.model';

@Injectable({
  providedIn: 'root'
})
export class AiService {
  private readonly apiUrl = `${environment.apiBaseUrl}/api/v1/ai`;

  constructor(private readonly http: HttpClient) {}

  chat(message: string, history?: ChatMessage[]): Observable<ChatResponse> {
    return this.http.post<ChatResponse>(`${this.apiUrl}/chat`, { message, history });
  }
}
