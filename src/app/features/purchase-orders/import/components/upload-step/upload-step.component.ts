import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { SelectModule } from 'primeng/select';
import { FileUploadModule } from 'primeng/fileupload';
import { ButtonModule } from 'primeng/button';
import { MessageModule } from 'primeng/message';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { LucideAngularModule, UploadCloud, FileSpreadsheet, X, AlertTriangle } from 'lucide-angular';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { MessageService, ConfirmationService } from 'primeng/api';
import { PoImportStoreService } from '../../services/po-import-store.service';
import { PurchaseOrderApiService } from '../../../services/purchase-order-api.service';
import { VendorService } from '../../../../vendors/services/vendor.service';
import { Vendor } from '../../../../vendors/models/vendor.model';
import { ExcelPreviewPanelComponent } from '../excel-preview-panel/excel-preview-panel.component';

@Component({
  selector: 'app-upload-step',
  standalone: true,
  imports: [
    CommonModule, 
    FormsModule, 
    SelectModule, 
    FileUploadModule, 
    ButtonModule, 
    MessageModule, 
    ProgressSpinnerModule, 
    LucideAngularModule,
    ExcelPreviewPanelComponent,
    ToastModule,
    ConfirmDialogModule
  ],
  providers: [MessageService, ConfirmationService],
  template: `
    <div class="flex flex-col gap-6 w-full relative">
      <p-toast></p-toast>
      <p-confirmdialog></p-confirmdialog>
      <!-- Vendor Selection -->
      <div class="flex flex-col gap-2">
        <label class="font-medium text-surface-900 dark:text-surface-0">Select Vendor <span class="text-red-500">*</span></label>
        <p-select 
          [options]="vendors()" 
          [ngModel]="store.selectedVendor()"
          (ngModelChange)="onVendorSelect($event)"
          optionLabel="name" 
          optionValue="id" 
          placeholder="Select a vendor" 
          [filter]="true" 
          filterPlaceholder="Search vendors..."
          [loading]="loadingVendors()"
          appendTo="body"
          styleClass="w-full sm:max-w-md">
        </p-select>
      </div>

      <!-- File Upload Area -->
      <div class="flex flex-col gap-2">
        <label class="font-medium text-surface-900 dark:text-surface-0">Upload Excel File <span class="text-red-500">*</span></label>
        
        <div *ngIf="!store.selectedFile()" 
             class="border-2 border-dashed border-surface-300 dark:border-surface-600 rounded-xl p-8 flex flex-col items-center justify-center bg-surface-50 dark:bg-surface-800 hover:bg-surface-100 dark:hover:bg-surface-700 transition-colors cursor-pointer"
             (click)="fileInput.click()"
             (dragover)="onDragOver($event)"
             (dragleave)="onDragLeave($event)"
             (drop)="onDrop($event)"
             [ngClass]="{'border-primary-500 bg-primary-50 dark:bg-primary-900/20': isDragging()}">
          <lucide-icon [img]="UploadCloudIcon" class="w-12 h-12 text-surface-400 mb-4"></lucide-icon>
          <p class="text-lg font-medium text-surface-700 dark:text-surface-200">Click or drag file to this area to upload</p>
          <p class="text-sm text-surface-500 mt-1">Supports .xlsx and .xls formats</p>
          <input #fileInput type="file" class="hidden" accept=".xlsx, .xls" (change)="onFileSelected($event)">
        </div>

        <div *ngIf="store.selectedFile()" 
             class="flex items-center justify-between p-4 border border-surface-200 dark:border-surface-700 rounded-xl bg-surface-0 dark:bg-surface-900 cursor-pointer hover:bg-surface-50 dark:hover:bg-surface-800 transition-colors"
             (click)="fileInput.click()">
          <div class="flex items-center gap-4">
            <div class="p-3 bg-green-100 dark:bg-green-900/40 text-green-600 dark:text-green-400 rounded-lg">
              <lucide-icon [img]="FileSpreadsheetIcon" class="w-6 h-6"></lucide-icon>
            </div>
            <div>
              <p class="font-medium text-surface-900 dark:text-surface-0">{{ store.selectedFile()?.name }}</p>
              <p class="text-sm text-surface-500">{{ formatFileSize(store.selectedFile()?.size) }}</p>
              <p class="text-xs text-surface-400 mt-0.5">(click anywhere to replace file)</p>
            </div>
          </div>
          <button type="button" class="text-red-500 hover:text-red-700 shrink-0" (click)="removeFile($event)">
            <lucide-icon [img]="XIcon" class="w-5 h-5"></lucide-icon>
          </button>
          <!-- Hidden file input for replacement -->
          <input #fileInput type="file" class="hidden" accept=".xlsx, .xls" (change)="onFileSelected($event)">
        </div>
      </div>

      <!-- Preview Section -->
      <div *ngIf="store.previewLoading()" class="flex flex-col items-center justify-center p-12">
        <p-progress-spinner styleClass="w-12 h-12" strokeWidth="4"></p-progress-spinner>
        <p class="mt-4 text-surface-600 font-medium">Extracting preview data...</p>
      </div>

      <div *ngIf="errorMsg()" class="p-4 bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-xl flex items-start gap-3">
        <lucide-icon [img]="AlertTriangleIcon" class="text-red-500 mt-0.5 w-5 h-5"></lucide-icon>
        <div class="text-red-700 dark:text-red-400 flex-1">
          <p class="font-medium">Failed to process file</p>
          <p class="text-sm mt-1">{{ errorMsg() }}</p>
        </div>
      </div>

      <div *ngIf="store.hasPreview()" class="flex flex-col gap-4 transition-opacity duration-200" [class.pointer-events-none]="store.previewLoading()" [class.opacity-50]="store.previewLoading()">
        
        <app-excel-preview-panel
          [data]="store.previewData()"
          [selectedRow]="store.selectedHeaderRow()"
          (rowSelected)="onHeaderRowSelected($event)">
        </app-excel-preview-panel>
      </div>
    </div>
  `
})
export class UploadStepComponent implements OnInit {
  store = inject(PoImportStoreService);
  private api = inject(PurchaseOrderApiService);
  private vendorService = inject(VendorService);
  private messageService = inject(MessageService);
  private confirmationService = inject(ConfirmationService);

  vendors = signal<Vendor[]>([]);
  loadingVendors = signal(true);
  isDragging = signal(false);
  errorMsg = signal<string | null>(null);

  UploadCloudIcon = UploadCloud;
  FileSpreadsheetIcon = FileSpreadsheet;
  AlertTriangleIcon = AlertTriangle;
  XIcon = X;

  ngOnInit() {
    this.vendorService.getVendors(1, 100).subscribe({
      next: (res) => {
        if (res.success && res.data && Array.isArray(res.data.items)) {
          this.vendors.set(res.data.items);
        } else if (Array.isArray(res)) {
          this.vendors.set(res);
        } else if (res.data && Array.isArray(res.data)) {
          this.vendors.set(res.data);
        }
        this.loadingVendors.set(false);
      },
      error: (err) => {
        console.error('[DEBUG] Vendor API error:', err);
        this.loadingVendors.set(false);
      }
    });
  }

  onVendorSelect(vendorId: string) {
    this.store.setVendor(vendorId);
  }

  onDragOver(event: DragEvent) {
    event.preventDefault();
    this.isDragging.set(true);
  }

  onDragLeave(event: DragEvent) {
    event.preventDefault();
    this.isDragging.set(false);
  }

  onDrop(event: DragEvent) {
    event.preventDefault();
    this.isDragging.set(false);
    if (event.dataTransfer?.files?.length) {
      this.handleFile(event.dataTransfer.files[0]);
    }
  }

  onFileSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    if (input.files?.length) {
      this.handleFile(input.files[0]);
    }
  }

  private handleFile(file: File) {
    if (!file.name.endsWith('.xlsx') && !file.name.endsWith('.xls')) {
      this.errorMsg.set('Invalid file type. Please upload an Excel file (.xlsx or .xls)');
      return;
    }

    const isReplacement = !!this.store.selectedFile();
    this.store.replaceFile(file);
    
    if (isReplacement) {
      this.messageService.add({ severity: 'success', summary: 'Success', detail: 'File replaced successfully.' });
    }

    this.errorMsg.set(null);
    this.store.setPreviewLoading(true);

    this.api.previewImport(file).subscribe({
      next: (res: any) => {
        if (res.success && res.data) {
          // Robust fallback just in case the backend returns raw array on data
          if (Array.isArray(res.data)) {
            this.store.setPreviewData(res.data, res.data.length);
          } else if (res.data.dataGrid && Array.isArray(res.data.dataGrid)) {
            this.store.setPreviewData(res.data.dataGrid, res.data.totalRowsFound || res.data.dataGrid.length);
          } else {
            this.errorMsg.set('Unexpected format returned from server.');
            this.store.removeFile();
          }
        } else if (Array.isArray(res)) {
          this.store.setPreviewData(res as string[][], res.length);
        } else {
          this.errorMsg.set(res.message || 'Failed to extract preview data from the file.');
          this.store.removeFile();
        }
        this.store.setPreviewLoading(false);
      },
      error: (err: any) => {
        this.errorMsg.set(err.error?.message || 'Failed to extract preview data from the file.');
        this.store.setPreviewLoading(false);
        this.store.removeFile(); // Reset on error
      }
    });
  }

  removeFile(event?: MouseEvent) {
    if (event) {
      event.stopPropagation();
    }

    this.confirmationService.confirm({
      message: 'Are you sure you want to remove this file? All preview data and mappings will be reset.',
      header: 'Confirm Removal',
      icon: 'pi pi-exclamation-triangle',
      accept: () => {
        this.store.removeFile();
        this.errorMsg.set(null);
        this.messageService.add({ severity: 'success', summary: 'Success', detail: 'File removed successfully.' });
      }
    });
  }

  onHeaderRowSelected(index: number) {
    this.store.setSelectedHeaderRow(index);
  }

  formatFileSize(bytes?: number): string {
    if (!bytes) return '0 B';
    const k = 1024;
    const sizes = ['B', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
  }
}
