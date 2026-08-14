import { Component, ChangeDetectionStrategy, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { ConfirmationService } from 'primeng/api';
import { LucideDynamicIcon, LucidePlus as Plus, LucideMessageSquare as MessageSquare, LucideTrash2 as Trash2, LucideEdit2 as Edit2, LucideCheck as Check, LucideX as X, LucideSearch as Search } from '@lucide/angular';
import { AiChatStore } from '../../state/ai-chat.store';
import { AiChatApiService } from '../../services/ai-chat-api.service';
import { toast } from 'ngx-sonner';

@Component({
  selector: 'app-ai-chat-sidebar',
  standalone: true,
  imports: [CommonModule, FormsModule, ButtonModule, InputTextModule, ProgressSpinnerModule, LucideDynamicIcon],
  template: `
    <div class="flex flex-col h-full bg-gray-50 dark:bg-gray-900/50">
      <div class="p-4 border-b dark:border-gray-800 flex items-center justify-between">
        <h2 class="text-lg font-semibold text-gray-800 dark:text-gray-100">AI Assistant</h2>
        <p-button 
          [text]="true"
          [rounded]="true"
          (onClick)="createNewChat()"
          [disabled]="store.isCreatingSession()"
          title="New Chat">
          <svg lucideIcon="plus" [style.width.px]="20" [style.height.px]="20"></svg>
        </p-button>
      </div>

      <div class="p-4 border-b dark:border-gray-800">
        <div class="relative">
          <svg lucideIcon="search" [style.width.px]="16" [style.height.px]="16" class="absolute right-3 top-1/2 -translate-y-1/2 text-gray-400"></svg>
          <input 
            pInputText 
            [(ngModel)]="searchQuery" 
            placeholder="Search chats..." 
            class="w-full !pr-10 py-2 text-sm border-gray-300 dark:border-gray-700 bg-white dark:bg-gray-800 rounded-md" 
          />
        </div>
      </div>

      <div class="flex-1 overflow-y-auto p-2 space-y-1">
        @if (store.isLoadingSessions() && !store.sessions().length) {
          <div class="flex justify-center p-4">
            <p-progress-spinner styleClass="w-6 h-6" strokeWidth="4"></p-progress-spinner>
          </div>
        } @else if (filteredSessions().length === 0) {
          <div class="text-center p-6 text-sm text-gray-500">
            @if (searchQuery()) {
              No conversations found.
            } @else {
              Start a new conversation with the Procurement Assistant.
            }
          </div>
        } @else {
          @for (session of filteredSessions(); track session.id) {
            <div 
              class="group relative flex items-center gap-3 p-3 rounded-lg cursor-pointer transition-colors"
              [class.bg-primary-50]="store.activeSessionId() === session.id"
              [class.dark:bg-primary-900]="store.activeSessionId() === session.id"
              [class.text-primary-700]="store.activeSessionId() === session.id"
              [class.dark:text-primary-300]="store.activeSessionId() === session.id"
              [class.hover:bg-gray-100]="store.activeSessionId() !== session.id"
              [class.dark:hover:bg-gray-800]="store.activeSessionId() !== session.id"
              (click)="selectSession(session.id)">
              
              <svg lucideIcon="message-square" [style.width.px]="18" [style.height.px]="18" class="shrink-0 text-gray-400" 
                [class.text-primary-500]="store.activeSessionId() === session.id"></svg>
              
              @if (editingSessionId() === session.id) {
                <div class="flex-1 flex items-center gap-1 min-w-0" (click)="$event.stopPropagation()">
                  <input 
                    pInputText 
                    #renameInput
                    [(ngModel)]="editTitle" 
                    (keydown.enter)="saveRename(session.id)"
                    (keydown.escape)="cancelRename()"
                    class="w-full py-1 px-2 text-sm"
                    autofocus />
                  <button class="p-1 text-green-600 hover:bg-green-50 rounded" (click)="saveRename(session.id)">
                    <svg lucideIcon="check" [style.width.px]="16" [style.height.px]="16"></svg>
                  </button>
                  <button class="p-1 text-red-600 hover:bg-red-50 rounded" (click)="cancelRename()">
                    <svg lucideIcon="x" [style.width.px]="16" [style.height.px]="16"></svg>
                  </button>
                </div>
              } @else {
                <div class="flex-1 min-w-0">
                  <div class="font-medium truncate text-sm" [title]="session.title || 'New Chat'">
                    {{ session.title || 'New Chat' }}
                  </div>
                </div>

                <div class="opacity-0 group-hover:opacity-100 flex items-center shrink-0 transition-opacity" (click)="$event.stopPropagation()">
                  <button 
                    class="p-1.5 text-gray-500 hover:text-primary-600 hover:bg-white dark:hover:bg-gray-700 rounded transition-colors" 
                    (click)="startRename(session.id, session.title)"
                    title="Rename">
                    <svg lucideIcon="edit-2" [style.width.px]="14" [style.height.px]="14"></svg>
                  </button>
                  <button 
                    class="p-1.5 text-gray-500 hover:text-red-600 hover:bg-white dark:hover:bg-gray-700 rounded transition-colors" 
                    (click)="confirmDelete(session.id, $event)"
                    title="Delete">
                    <svg lucideIcon="trash-2" [style.width.px]="14" [style.height.px]="14"></svg>
                  </button>
                </div>
              }
            </div>
          }
        }
      </div>
    </div>
  `,
  styles: [],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AiChatSidebarComponent {
  readonly store = inject(AiChatStore);
  private readonly api = inject(AiChatApiService);
  private readonly confirmationService = inject(ConfirmationService);

  readonly searchQuery = signal('');
  
  readonly editingSessionId = signal<string | null>(null);
  editTitle = '';

  // Make lucide icons available to template
  readonly Plus = Plus;
  readonly MessageSquare = MessageSquare;
  readonly Trash2 = Trash2;
  readonly Edit2 = Edit2;
  readonly Check = Check;
  readonly X = X;
  readonly Search = Search;

  get filteredSessions() {
    return () => {
      const q = this.searchQuery().toLowerCase();
      const sessions = this.store.sessions();
      if (!q) return sessions;
      return sessions.filter(s => (s.title || '').toLowerCase().includes(q));
    };
  }

  createNewChat() {
    this.store.createSession('New Chat');
  }

  selectSession(id: string) {
    if (this.editingSessionId() !== id) {
      this.store.selectSession(id);
    }
  }

  startRename(id: string, currentTitle: string) {
    this.editingSessionId.set(id);
    this.editTitle = currentTitle || 'New Chat';
  }

  cancelRename() {
    this.editingSessionId.set(null);
  }

  saveRename(id: string) {
    const newTitle = this.editTitle.trim();
    if (!newTitle) {
      this.cancelRename();
      return;
    }

    // Optimistically update locally
    this.store.updateSessionTitleLocally(id, newTitle);
    this.editingSessionId.set(null);

    this.api.updateSessionTitle(id, { title: newTitle }).subscribe({
      next: (res) => {
        if (!res.success) {
           toast.error('Failed to rename session.');
           this.store.loadSessions(); // Re-sync if failed
        }
      },
      error: () => {
         toast.error('Failed to rename session.');
         this.store.loadSessions(); // Re-sync if failed
      }
    });
  }

  confirmDelete(id: string, event: Event) {
    event.stopPropagation();
    this.confirmationService.confirm({
      message: 'Are you sure you want to delete this conversation?',
      header: 'Delete Confirmation',
      icon: 'pi pi-exclamation-triangle',
      acceptButtonStyleClass: 'p-button-danger',
      accept: () => {
        this.api.deleteSession(id).subscribe({
          next: (res) => {
            if (res.success) {
               this.store.deleteSessionLocally(id);
               toast.success('Session deleted successfully.');
            }
          },
          error: () => {
             toast.error('Failed to delete session.');
          }
        });
      }
    });
  }
}
