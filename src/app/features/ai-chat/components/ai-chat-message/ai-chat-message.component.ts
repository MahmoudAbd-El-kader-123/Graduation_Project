import { Component, ChangeDetectionStrategy, Input, OnChanges, SimpleChanges, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { ChatMessageDto } from '../../models/ai-chat.model';
import { LucideAngularModule, User, Bot } from 'lucide-angular';
import { marked } from 'marked';
import DOMPurify from 'dompurify';

@Component({
  selector: 'app-ai-chat-message',
  standalone: true,
  imports: [CommonModule, LucideAngularModule],
  template: `
    <div class="flex gap-4 p-6" [class.bg-gray-50]="isAssistant" [class.dark:bg-gray-800/50]="isAssistant">
      <div class="flex-shrink-0 mt-1">
        <div class="w-8 h-8 rounded-full flex items-center justify-center" 
             [class.bg-primary-600]="!isAssistant" 
             [class.text-white]="!isAssistant"
             [class.bg-gray-200]="isAssistant"
             [class.text-gray-700]="isAssistant"
             [class.dark:bg-gray-700]="isAssistant"
             [class.dark:text-gray-200]="isAssistant">
          <lucide-icon [name]="isAssistant ? 'Bot' : 'User'" [size]="18"></lucide-icon>
        </div>
      </div>
      <div class="flex-1 min-w-0">
        <div class="font-medium text-sm text-gray-900 dark:text-gray-100 mb-1">
          {{ isAssistant ? 'Procurement Assistant' : 'You' }}
        </div>
        
        @if (isAssistant) {
          <!-- Render sanitized Markdown HTML for Assistant -->
          <div class="text-gray-700 dark:text-gray-300 text-sm leading-relaxed break-words markdown-body" [innerHTML]="parsedHtml"></div>
        } @else {
          <!-- Render raw plain text for User -->
          <div class="text-gray-700 dark:text-gray-300 text-sm leading-relaxed whitespace-pre-wrap break-words">{{ message.content }}</div>
        }
      </div>
    </div>
  `,
  styles: [`
    /* Scoped Markdown Styles */
    ::ng-deep .markdown-body p { margin-bottom: 0.75rem; }
    ::ng-deep .markdown-body p:last-child { margin-bottom: 0; }
    
    ::ng-deep .markdown-body strong { font-weight: 600; color: inherit; }
    ::ng-deep .markdown-body em { font-style: italic; }
    
    ::ng-deep .markdown-body ul { list-style-type: disc; padding-left: 1.5rem; margin-bottom: 0.75rem; }
    ::ng-deep .markdown-body ol { list-style-type: decimal; padding-left: 1.5rem; margin-bottom: 0.75rem; }
    ::ng-deep .markdown-body li { margin-bottom: 0.25rem; }
    ::ng-deep .markdown-body li > p { margin-bottom: 0.25rem; display: inline-block; }
    
    ::ng-deep .markdown-body code { 
      background-color: rgba(156, 163, 175, 0.2); 
      padding: 0.125rem 0.25rem; 
      border-radius: 0.25rem; 
      font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, "Liberation Mono", "Courier New", monospace; 
      font-size: 0.875em; 
    }
    
    ::ng-deep .markdown-body pre {
      background-color: #1f2937;
      color: #e5e7eb;
      padding: 1rem;
      border-radius: 0.5rem;
      overflow-x: auto;
      margin-bottom: 0.75rem;
    }
    ::ng-deep .markdown-body pre code {
      background-color: transparent;
      padding: 0;
      color: inherit;
      font-size: 0.875em;
    }
    
    ::ng-deep .markdown-body a {
      color: var(--primary-600, #2563eb);
      text-decoration: underline;
      text-underline-offset: 2px;
    }
    ::ng-deep .dark .markdown-body a {
      color: var(--primary-400, #60a5fa);
    }
    
    ::ng-deep .markdown-body blockquote {
      border-left: 4px solid #e5e7eb;
      padding-left: 1rem;
      color: #6b7280;
      margin-bottom: 0.75rem;
      font-style: italic;
    }
    ::ng-deep .dark .markdown-body blockquote {
      border-left-color: #4b5563;
      color: #9ca3af;
    }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AiChatMessageComponent implements OnChanges {
  @Input({ required: true }) message!: ChatMessageDto;

  readonly User = User;
  readonly Bot = Bot;
  
  private readonly sanitizer = inject(DomSanitizer);
  
  parsedHtml: SafeHtml = '';

  get isAssistant(): boolean {
    return this.message.role?.toLowerCase() === 'assistant' || this.message.role?.toLowerCase() === 'system';
  }

  ngOnChanges(changes: SimpleChanges) {
    if (changes['message']) {
      this.updateParsedContent();
    }
  }

  private updateParsedContent() {
    if (!this.message?.content) {
      this.parsedHtml = '';
      return;
    }
    
    if (!this.isAssistant) {
      return;
    }
    
    // 1. Parse Markdown securely
    const rawHtml = marked.parse(this.message.content, { async: false, breaks: true }) as string;
    
    // 2. Sanitize HTML using DOMPurify
    const cleanHtml = DOMPurify.sanitize(rawHtml, {
      USE_PROFILES: { html: true },
      FORBID_TAGS: ['style', 'script', 'iframe', 'object', 'embed'],
      FORBID_ATTR: ['style', 'on*']
    });
    
    // 3. Mark as trusted so Angular will render it via [innerHTML]
    this.parsedHtml = this.sanitizer.bypassSecurityTrustHtml(cleanHtml);
  }
}
