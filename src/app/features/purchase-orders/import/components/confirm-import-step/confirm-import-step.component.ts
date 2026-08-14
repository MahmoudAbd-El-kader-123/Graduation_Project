import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ButtonModule } from 'primeng/button';
import { LucideDynamicIcon, LucideCheckCircle2 as CheckCircle2, LucideFileText as FileText, LucideBuilding2 as Building2, LucideMap as Map } from '@lucide/angular';
import { PoImportStoreService } from '../../services/po-import-store.service';
import { MappingTableComponent } from '../mapping-table/mapping-table.component';
import { IMPORT_SYSTEM_FIELDS } from '../../../../../shared/constants/import-system-fields';

@Component({
  selector: 'app-confirm-import-step',
  standalone: true,
  imports: [CommonModule, ButtonModule, MappingTableComponent, LucideDynamicIcon],
  template: `
    <div class="flex flex-col gap-6">
      <div>
        <h3 class="text-lg font-semibold text-surface-900 dark:text-surface-0 flex items-center gap-2">
          <svg [lucideIcon]="CheckCircle2Icon" class="w-5 h-5 text-primary-500"></svg>
          Confirm Import
        </h3>
        <p class="text-sm text-surface-500 mt-1">Review your import settings before proceeding.</p>
      </div>

      <div class="grid grid-cols-1 md:grid-cols-3 gap-4">
        <!-- Vendor Card -->
        <div class="bg-surface-0 dark:bg-surface-900 border border-surface-200 dark:border-surface-700 rounded-xl p-4 flex gap-4 items-start">
          <div class="w-10 h-10 rounded-lg bg-primary-50 dark:bg-primary-900/50 flex items-center justify-center shrink-0">
            <svg [lucideIcon]="Building2Icon" class="w-5 h-5 text-primary-600 dark:text-primary-400"></svg>
          </div>
          <div>
            <p class="text-xs font-medium text-surface-500 uppercase tracking-wider">Vendor</p>
            <p class="font-medium text-surface-900 dark:text-surface-0 mt-1">{{ store.selectedVendor() || 'Unknown' }}</p>
          </div>
        </div>

        <!-- File Card -->
        <div class="bg-surface-0 dark:bg-surface-900 border border-surface-200 dark:border-surface-700 rounded-xl p-4 flex gap-4 items-start">
          <div class="w-10 h-10 rounded-lg bg-blue-50 dark:bg-blue-900/50 flex items-center justify-center shrink-0">
            <svg [lucideIcon]="FileTextIcon" class="w-5 h-5 text-blue-600 dark:text-blue-400"></svg>
          </div>
          <div class="min-w-0 flex-1">
            <p class="text-xs font-medium text-surface-500 uppercase tracking-wider">Source File</p>
            <p class="font-medium text-surface-900 dark:text-surface-0 mt-1 truncate" [title]="store.selectedFile()?.name">
              {{ store.selectedFile()?.name }}
            </p>
            <p class="text-xs text-surface-500 mt-0.5">{{ store.totalRowsFound() }} rows detected</p>
          </div>
        </div>

        <!-- Mappings Card -->
        <div class="bg-surface-0 dark:bg-surface-900 border border-surface-200 dark:border-surface-700 rounded-xl p-4 flex gap-4 items-start">
          <div class="w-10 h-10 rounded-lg bg-purple-50 dark:bg-purple-900/50 flex items-center justify-center shrink-0">
            <svg [lucideIcon]="MapIcon" class="w-5 h-5 text-purple-600 dark:text-purple-400"></svg>
          </div>
          <div>
            <p class="text-xs font-medium text-surface-500 uppercase tracking-wider">Mappings</p>
            <p class="font-medium text-surface-900 dark:text-surface-0 mt-1">{{ store.mappings().length }} columns mapped</p>
            <p class="text-xs text-surface-500 mt-0.5">Header is on row {{ (store.selectedHeaderRow() || 0) + 1 }}</p>
          </div>
        </div>
      </div>

      <!-- Mapping Summary -->
      <div>
        <h4 class="text-sm font-semibold text-surface-900 dark:text-surface-0 mb-3 uppercase tracking-wider">Mapping Summary</h4>
        <div class="bg-surface-0 dark:bg-surface-900 border border-surface-200 dark:border-surface-700 rounded-xl overflow-hidden p-4">
          <app-mapping-table
            [systemFields]="systemFields"
            [excelHeaders]="store.extractedColumns()"
            [mappings]="store.mappings()"
            [readonly]="true">
          </app-mapping-table>
        </div>
      </div>
    </div>
  `
})
export class ConfirmImportStepComponent {
  store = inject(PoImportStoreService);
  
  systemFields = IMPORT_SYSTEM_FIELDS;

  CheckCircle2Icon = CheckCircle2;
  FileTextIcon = FileText;
  Building2Icon = Building2;
  MapIcon = Map;
}
