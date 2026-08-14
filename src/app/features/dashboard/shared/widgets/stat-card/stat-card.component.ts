import { Component, input, computed } from '@angular/core';
import { LucideDynamicIcon } from '@lucide/angular';

@Component({
  selector: 'app-stat-card',
  standalone: true,
  imports: [LucideDynamicIcon],
  template: `
    <div class="bg-surface-0 dark:bg-surface-900 p-6 rounded-xl shadow-sm border border-surface-200 dark:border-surface-700 h-full flex flex-col justify-between">
      <div class="flex justify-between items-start mb-4">
        <div>
          <span class="block text-surface-500 font-medium mb-1">{{ title() }}</span>
          <div class="text-3xl font-bold text-surface-900 dark:text-surface-0">{{ value() }}</div>
        </div>
        <div class="w-12 h-12 rounded-lg bg-primary/10 text-primary flex items-center justify-center">
          <svg [lucideIcon]="icon()" [style.width.px]="24" [style.height.px]="24"></svg>
        </div>
      </div>
      
      @if (trend()) {
        <div class="flex items-center gap-2 text-sm">
          <span [class]="trend()!.isPositive ? 'text-green-500 font-medium' : 'text-red-500 font-medium'">
            {{ trend()!.isPositive ? '+' : '-' }}{{ trend()!.value }}%
          </span>
          <span class="text-surface-500">vs last month</span>
        </div>
      }
    </div>
  `
})
export class StatCardComponent {
  readonly title = input.required<string>();
  readonly value = input.required<string | number>();
  readonly colorClass = input<'primary' | 'success' | 'warning' | 'danger'>('primary');

  readonly icon = input.required<string>();
  readonly trend = input<{ value: number; isPositive: boolean }>();
}
