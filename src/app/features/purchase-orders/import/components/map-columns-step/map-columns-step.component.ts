import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MessageModule } from 'primeng/message';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { LucideAngularModule, GitMerge, AlertCircle, CheckCircle2 } from 'lucide-angular';
import { PoImportStoreService } from '../../services/po-import-store.service';
import { MappingTableComponent } from '../mapping-table/mapping-table.component';
import { IMPORT_SYSTEM_FIELDS } from '../../../../../shared/constants/import-system-fields';
import { ColumnMapping } from '../../../models/vendor-mapping.model';

@Component({
  selector: 'app-map-columns-step',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MessageModule,
    ProgressSpinnerModule,
    LucideAngularModule,
    MappingTableComponent
  ],
  template: `
    <div class="flex flex-col gap-6">
      <div class="flex items-center justify-between">
        <div>
          <h3 class="text-lg font-semibold text-surface-900 dark:text-surface-0 flex items-center gap-2">
            <lucide-icon [img]="GitMergeIcon" class="w-5 h-5 text-primary-500"></lucide-icon>
            Map Excel Columns
          </h3>
          <p class="text-sm text-surface-500 mt-1">Match your Excel columns to the required system fields.</p>
        </div>
        
        <div class="flex items-center gap-2 text-sm font-medium" 
             [ngClass]="store.isMappingValid() ? 'text-green-600 dark:text-green-400' : 'text-orange-500'">
          <lucide-icon [img]="store.isMappingValid() ? CheckCircle2Icon : AlertCircleIcon" class="w-4 h-4"></lucide-icon>
          {{ store.isMappingValid() ? 'Ready to proceed' : 'Mapping incomplete' }}
        </div>
      </div>

      <div *ngIf="store.mappingLoading()" class="flex flex-col items-center justify-center p-8">
        <p-progress-spinner styleClass="w-10 h-10" strokeWidth="4"></p-progress-spinner>
        <p class="mt-4 text-surface-600 font-medium">Loading previous mappings...</p>
      </div>

      <div *ngIf="!store.mappingLoading()" class="bg-surface-0 dark:bg-surface-900 border border-surface-200 dark:border-surface-700 rounded-xl p-4">
        <app-mapping-table
          [systemFields]="systemFields"
          [excelHeaders]="store.extractedColumns()"
          [mappings]="store.mappings()"
          [readonly]="false"
          (mappingsChange)="onMappingsChange($event)">
        </app-mapping-table>
      </div>
    </div>
  `
})
export class MapColumnsStepComponent implements OnInit {
  store = inject(PoImportStoreService);

  systemFields = IMPORT_SYSTEM_FIELDS;

  GitMergeIcon = GitMerge;
  AlertCircleIcon = AlertCircle;
  CheckCircle2Icon = CheckCircle2;

  ngOnInit() {
    const vendorId = this.store.selectedVendor();
    // Only load mappings from backend if we haven't already started mapping
    // This preserves user's selections when navigating back and forth between steps
    if (vendorId && this.store.mappings().length === 0) {
      this.store.loadVendorMappings(vendorId);
    }
  }

  onMappingsChange(mappings: ColumnMapping[]) {
    this.store.setMappings(mappings);
  }
}
