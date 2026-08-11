import { Component, ChangeDetectionStrategy, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AiChatStore } from '../../state/ai-chat.store';
import { AiChatSidebarComponent } from '../ai-chat-sidebar/ai-chat-sidebar.component';
import { AiChatAreaComponent } from '../ai-chat-area/ai-chat-area.component';
import { ConfirmDialog } from 'primeng/confirmdialog';
import { ConfirmationService } from 'primeng/api';

@Component({
  selector: 'app-ai-chat-layout',
  standalone: true,
  imports: [CommonModule, AiChatSidebarComponent, AiChatAreaComponent, ConfirmDialog],
  providers: [ConfirmationService],
  template: `
    <div class="flex h-[calc(100vh-theme(spacing.16))] bg-white dark:bg-gray-900 border-t dark:border-gray-800">
      <app-ai-chat-sidebar class="w-full md:w-80 border-r dark:border-gray-800 hidden md:flex flex-col h-full shrink-0"></app-ai-chat-sidebar>
      <app-ai-chat-area class="flex-1 flex flex-col h-full min-w-0"></app-ai-chat-area>
    </div>
    
    <!-- Global confirm dialog for delete confirmation in AI Chat -->
    <p-confirmdialog [style]="{width: '450px'}"></p-confirmdialog>
  `,
  styles: [],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AiChatLayoutComponent implements OnInit {
  private readonly store = inject(AiChatStore);

  ngOnInit() {
    this.store.loadSessions();
  }
}
