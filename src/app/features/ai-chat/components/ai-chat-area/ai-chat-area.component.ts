import { Component, ChangeDetectionStrategy, inject, ViewChild, ElementRef, AfterViewChecked, signal, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { LucideAngularModule, Send, AlertCircle, RefreshCw } from 'lucide-angular';
import { AiChatStore } from '../../state/ai-chat.store';
import { AiChatMessageComponent } from '../ai-chat-message/ai-chat-message.component';

@Component({
  selector: 'app-ai-chat-area',
  standalone: true,
  imports: [CommonModule, FormsModule, ButtonModule, ProgressSpinnerModule, LucideAngularModule, AiChatMessageComponent],
  template: `
    @if (!store.activeSessionId()) {
      <div class="flex-1 flex flex-col items-center justify-center p-8 text-center text-gray-500">
        <div class="w-16 h-16 bg-primary-50 dark:bg-primary-900/50 rounded-full flex items-center justify-center mb-4">
          <lucide-icon name="Bot" [size]="32" class="text-primary-600 dark:text-primary-400"></lucide-icon>
        </div>
        <h3 class="text-xl font-semibold text-gray-800 dark:text-gray-100 mb-2">Procurement Intelligence Assistant</h3>
        <p class="max-w-md">
          Ask a question about vendors, purchase orders, invoices, discrepancies, or procurement performance.
        </p>
        <div class="mt-8 flex flex-col gap-2 max-w-sm w-full">
          <button (click)="quickPrompt('Which vendor increased prices the most this month?')" 
                  class="text-left px-4 py-3 bg-gray-50 dark:bg-gray-800 hover:bg-gray-100 dark:hover:bg-gray-700 rounded-lg text-sm transition-colors border dark:border-gray-700">
            "Which vendor increased prices the most this month?"
          </button>
          <button (click)="quickPrompt('Which suppliers have the highest discrepancy rates?')" 
                  class="text-left px-4 py-3 bg-gray-50 dark:bg-gray-800 hover:bg-gray-100 dark:hover:bg-gray-700 rounded-lg text-sm transition-colors border dark:border-gray-700">
            "Which suppliers have the highest discrepancy rates?"
          </button>
          <button (click)="quickPrompt('Which invoices are pending approval?')" 
                  class="text-left px-4 py-3 bg-gray-50 dark:bg-gray-800 hover:bg-gray-100 dark:hover:bg-gray-700 rounded-lg text-sm transition-colors border dark:border-gray-700">
            "Which invoices are pending approval?"
          </button>
        </div>
      </div>
    } @else {
      <!-- Header (Mobile mostly, or context) -->
      <div class="h-14 border-b dark:border-gray-800 flex items-center px-6 shrink-0 bg-white dark:bg-gray-900">
        <h3 class="font-medium text-gray-800 dark:text-gray-100 truncate">
          {{ store.activeSession()?.title || 'New Chat' }}
        </h3>
      </div>

      <!-- Messages Area -->
      <div #scrollContainer class="flex-1 overflow-y-auto min-h-0 bg-white dark:bg-gray-900">
        @if (store.isLoadingSession()) {
          <div class="flex items-center justify-center h-full">
            <div class="flex flex-col items-center text-gray-500 gap-3">
              <p-progress-spinner styleClass="w-8 h-8" strokeWidth="4"></p-progress-spinner>
              <span class="text-sm">Loading conversation...</span>
            </div>
          </div>
        } @else {
          <div class="flex flex-col pb-6">
            @for (msg of store.messages(); track msg.id) {
              <app-ai-chat-message [message]="msg"></app-ai-chat-message>
            }
            
            @if (store.isSendingMessage()) {
              <div class="flex gap-4 p-6 bg-gray-50 dark:bg-gray-800/50">
                <div class="flex-shrink-0 mt-1">
                  <div class="w-8 h-8 rounded-full bg-gray-200 dark:bg-gray-700 text-gray-700 dark:text-gray-200 flex items-center justify-center">
                    <lucide-icon name="Bot" [size]="18"></lucide-icon>
                  </div>
                </div>
                <div class="flex-1 min-w-0 flex items-center gap-2 text-gray-500">
                  <p-progress-spinner styleClass="w-4 h-4" strokeWidth="4"></p-progress-spinner>
                  <span class="text-sm">AI is thinking...</span>
                </div>
              </div>
            }

            @if (store.messageError()) {
              <div class="flex gap-4 p-6 bg-red-50 dark:bg-red-900/10 border-t border-b border-red-100 dark:border-red-900/30">
                <div class="flex-shrink-0 mt-1">
                  <lucide-icon name="AlertCircle" class="text-red-600 dark:text-red-400" [size]="24"></lucide-icon>
                </div>
                <div class="flex-1 min-w-0">
                  <div class="text-red-800 dark:text-red-300 font-medium mb-1">
                    {{ store.messageError()?.message }}
                  </div>
                  @if (store.pendingUserMessage()) {
                    <div class="mt-3 flex gap-3">
                      <p-button 
                        size="small" 
                        severity="danger" 
                        (onClick)="retryMessage()" 
                        [disabled]="store.isSendingMessage()">
                        <div class="flex items-center gap-2">
                          <lucide-icon name="RefreshCw" [size]="14"></lucide-icon>
                          <span>Retry</span>
                        </div>
                      </p-button>
                      <p-button 
                        size="small" 
                        severity="secondary" 
                        [text]="true"
                        (onClick)="store.clearError()">
                        Dismiss
                      </p-button>
                    </div>
                  }
                </div>
              </div>
            }
          </div>
        }
      </div>

      <!-- Input Area -->
      <div class="p-4 bg-white dark:bg-gray-900 border-t dark:border-gray-800 shrink-0">
        <div class="max-w-4xl mx-auto relative rounded-xl border border-gray-300 dark:border-gray-700 shadow-sm bg-white dark:bg-gray-800 focus-within:ring-2 focus-within:ring-primary-500 focus-within:border-primary-500 transition-all">
          <textarea
            #messageInput
            [(ngModel)]="currentInput"
            (keydown)="onKeydown($event)"
            [disabled]="store.isSendingMessage()"
            placeholder="Type your message... (Shift+Enter for new line)"
            class="w-full max-h-48 min-h-[56px] py-3.5 pl-4 pr-12 bg-transparent border-none outline-none resize-none text-gray-900 dark:text-gray-100 text-sm"
            rows="1">
          </textarea>
          
          <p-button 
            class="absolute right-2 bottom-2"
            [disabled]="!currentInput.trim() || store.isSendingMessage()"
            (onClick)="sendMessage()">
            <lucide-icon name="Send" [size]="18"></lucide-icon>
          </p-button>
        </div>
        <div class="text-center mt-2">
          <span class="text-xs text-gray-400">AI can make mistakes. Verify important information.</span>
        </div>
      </div>
    }
  `,
  styles: [],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AiChatAreaComponent implements AfterViewChecked {
  readonly store = inject(AiChatStore);
  
  @ViewChild('scrollContainer') private scrollContainer!: ElementRef<HTMLDivElement>;
  
  readonly Send = Send;
  readonly AlertCircle = AlertCircle;
  readonly RefreshCw = RefreshCw;

  currentInput = '';
  private shouldScrollToBottom = false;

  constructor() {
    // When messages change, schedule a scroll
    effect(() => {
      this.store.messages();
      this.store.isSendingMessage();
      this.shouldScrollToBottom = true;
    });
  }

  ngAfterViewChecked() {
    if (this.shouldScrollToBottom) {
      this.scrollToBottom();
      this.shouldScrollToBottom = false;
    }
  }

  private scrollToBottom() {
    try {
      if (this.scrollContainer) {
        this.scrollContainer.nativeElement.scrollTop = this.scrollContainer.nativeElement.scrollHeight;
      }
    } catch (err) {}
  }

  onKeydown(event: KeyboardEvent) {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      this.sendMessage();
    }
  }

  sendMessage() {
    if (!this.currentInput.trim() || this.store.isSendingMessage()) return;
    
    // Auto-create session if sending message without an active session
    if (!this.store.activeSessionId()) {
       // Since the UI requires an active session, this branch won't typically hit 
       // due to the @if (!store.activeSessionId()) check wrapping the whole UI.
       return;
    }

    const text = this.currentInput;
    this.currentInput = '';
    this.store.sendMessage(text);
  }

  retryMessage() {
    this.store.retryMessage();
  }

  quickPrompt(prompt: string) {
    if (!this.store.activeSessionId()) {
      this.store.createSession('New Chat');
      // Pre-fill the input so the user can just hit send once the session is active
      this.currentInput = prompt;
    }
  }
}
