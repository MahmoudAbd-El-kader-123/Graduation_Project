import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { catchError, finalize, of, tap } from 'rxjs';
import { AiChatApiService } from '../services/ai-chat-api.service';
import {
  ChatSessionListDto,
  ChatSessionDetailDto,
  ChatMessageDto,
  ChatErrorState,
  ChatErrorKind
} from '../models/ai-chat.model';

@Injectable({
  providedIn: 'root'
})
export class AiChatStore {
  private readonly api = inject(AiChatApiService);

  // State
  private readonly _sessions = signal<ChatSessionListDto[]>([]);
  private readonly _activeSessionId = signal<string | null>(null);
  private readonly _messages = signal<ChatMessageDto[]>([]);
  private readonly _isLoadingSessions = signal(false);
  private readonly _isLoadingSession = signal(false);
  private readonly _isSendingMessage = signal(false);
  private readonly _isCreatingSession = signal(false);
  private readonly _messageError = signal<ChatErrorState | null>(null);
  
  // To keep track of the failed user message text so they can retry it without typing again
  private readonly _pendingUserMessage = signal<string | null>(null);

  // Selectors
  readonly sessions = this._sessions.asReadonly();
  readonly activeSessionId = this._activeSessionId.asReadonly();
  readonly messages = this._messages.asReadonly();
  readonly isLoadingSessions = this._isLoadingSessions.asReadonly();
  readonly isLoadingSession = this._isLoadingSession.asReadonly();
  readonly isSendingMessage = this._isSendingMessage.asReadonly();
  readonly isCreatingSession = this._isCreatingSession.asReadonly();
  readonly messageError = this._messageError.asReadonly();
  readonly pendingUserMessage = this._pendingUserMessage.asReadonly();

  readonly activeSession = computed(() => 
    this._sessions().find(s => s.id === this._activeSessionId()) || null
  );

  loadSessions() {
    this._isLoadingSessions.set(true);
    // For now, load page 1, size 100. In a real app we might paginate properly.
    this.api.getSessions({ pageNumber: 1, pageSize: 50 })
      .pipe(
        finalize(() => this._isLoadingSessions.set(false))
      )
      .subscribe({
        next: (res) => {
          if (res.success && res.data) {
            this._sessions.set(res.data.items);
          }
        },
        error: (err) => {
          console.error('Failed to load sessions', err);
        }
      });
  }

  createSession(title?: string) {
    if (this._isCreatingSession()) return;

    this._isCreatingSession.set(true);
    this.api.createSession({ title })
      .pipe(
        finalize(() => this._isCreatingSession.set(false))
      )
      .subscribe({
        next: (res) => {
          if (res.success && res.data) {
            // Add new session to top of list
            this._sessions.update(sessions => [res.data, ...sessions]);
            this.selectSession(res.data.id);
          }
        }
      });
  }

  selectSession(id: string) {
    if (this._activeSessionId() === id) return;

    this._activeSessionId.set(id);
    this._messages.set([]);
    this._messageError.set(null);
    this._pendingUserMessage.set(null);
    
    this._isLoadingSession.set(true);
    
    this.api.getSessionById(id)
      .pipe(
        finalize(() => {
           // We only clear loading state if we are still on the same session
           if (this._activeSessionId() === id) {
             this._isLoadingSession.set(false);
           }
        })
      )
      .subscribe({
        next: (res) => {
          if (res.success && res.data && this._activeSessionId() === id) {
            this._messages.set(res.data.messages || []);
          }
        },
        error: (err) => {
          if (err.status === 404) {
            // Session not found, remove from list and unselect
            this._sessions.update(sessions => sessions.filter(s => s.id !== id));
            if (this._activeSessionId() === id) {
               this._activeSessionId.set(null);
            }
          }
        }
      });
  }

  sendMessage(message: string) {
    const sessionId = this._activeSessionId();
    if (!sessionId || this._isSendingMessage() || !message.trim()) return;

    this._isSendingMessage.set(true);
    this._messageError.set(null);
    this._pendingUserMessage.set(message);

    // Optimistically add user message to UI
    const tempUserMessage: ChatMessageDto = {
      id: crypto.randomUUID(),
      role: 'User',
      content: message,
      createdAt: new Date().toISOString()
    };
    
    this._messages.update(msgs => [...msgs, tempUserMessage]);

    this.api.sendMessage(sessionId, { message })
      .pipe(
        finalize(() => {
          if (this._activeSessionId() === sessionId) {
             this._isSendingMessage.set(false);
          }
        })
      )
      .subscribe({
        next: (res) => {
          if (res.success && res.data && this._activeSessionId() === sessionId) {
            // The backend returns both userMessage and assistantMessage.
            // Replace the optimistic tempUserMessage with the real one, and add the AI response.
            this._messages.update(msgs => {
              const filtered = msgs.filter(m => m.id !== tempUserMessage.id);
              return [...filtered, res.data.userMessage, res.data.assistantMessage];
            });
            this._pendingUserMessage.set(null); // Success, clear pending
          }
        },
        error: (err: HttpErrorResponse) => {
          if (this._activeSessionId() === sessionId) {
            this._messageError.set(this.mapError(err));
            // Remove optimistic message so the user can see the error + their pending input to retry
            this._messages.update(msgs => msgs.filter(m => m.id !== tempUserMessage.id));
          }
        }
      });
  }

  retryMessage() {
    const pendingMsg = this._pendingUserMessage();
    if (pendingMsg) {
      this.sendMessage(pendingMsg);
    }
  }

  clearError() {
     this._messageError.set(null);
  }
  
  deleteSessionLocally(id: string) {
     this._sessions.update(sessions => sessions.filter(s => s.id !== id));
     if (this._activeSessionId() === id) {
         this._activeSessionId.set(null);
         this._messages.set([]);
     }
  }
  
  updateSessionTitleLocally(id: string, title: string) {
      this._sessions.update(sessions => 
          sessions.map(s => s.id === id ? { ...s, title } : s)
      );
  }

  private mapError(err: HttpErrorResponse): ChatErrorState {
    let kind: ChatErrorKind = 'unknown';
    let message = 'An unexpected error occurred.';

    if (err.status === 429) {
      kind = 'rate-limit';
      message = "You're sending messages too quickly. Please try again in a moment.";
    } else if (err.status === 503 || err.status === 504) {
      kind = 'ai-unavailable';
      message = "AI Assistant is temporarily unavailable. Your conversation is safe. Please try again in a moment.";
    } else if (err.status === 502) {
      kind = 'ai-unavailable';
      message = "AI returned an unexpected response. Please try again.";
    } else if (err.status === 404) {
      kind = 'not-found';
      message = "This conversation was not found.";
    } else if (err.status === 401) {
      kind = 'unauthorized';
      message = "Your session has expired.";
    } else if (err.status === 403) {
      kind = 'forbidden';
      message = "You don't have permission to use the AI Assistant.";
    } else if (err.status === 400) {
      kind = 'validation';
      // Use backend message if available and safe
      if (err.error && err.error.message) {
         message = err.error.message;
      } else {
         message = "Unable to process this request due to invalid input.";
      }
    } else if (err.status === 0) {
      kind = 'network';
      message = "Unable to connect to the server. Please check your connection and try again.";
    } else if (err.status >= 500) {
      kind = 'server';
      message = "An internal server error occurred.";
    }
    
    return { kind, message };
  }
}
