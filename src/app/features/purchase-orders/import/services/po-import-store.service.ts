import { Injectable, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { ColumnMapping, VendorMapping } from '../../models/vendor-mapping.model';
import { PurchaseOrderApiService } from '../../services/purchase-order-api.service';
import { PurchaseOrderStoreService } from '../../services/purchase-order-store.service';
import { MessageService } from 'primeng/api';
import { firstValueFrom } from 'rxjs';
import { IMPORT_SYSTEM_FIELDS } from '../../../../shared/constants/import-system-fields';

@Injectable({
  providedIn: 'root'
})
export class PoImportStoreService {
  private api = inject(PurchaseOrderApiService);
  private listStore = inject(PurchaseOrderStoreService);
  private messageService = inject(MessageService);
  private router = inject(Router);

  readonly selectedFile = signal<File | null>(null);
  readonly selectedVendor = signal<string | null>(null);
  
  // Preview State
  readonly previewData = signal<string[][]>([]);
  readonly totalRowsFound = signal<number>(0);
  readonly selectedHeaderRow = signal<number | null>(null);
  readonly previewLoading = signal<boolean>(false);
  
  // Mapping State
  readonly initialMappings = signal<ColumnMapping[]>([]);
  readonly mappings = signal<ColumnMapping[]>([]);
  readonly mappingLoading = signal<boolean>(false);
  readonly savingMappings = signal<boolean>(false);

  // Import State
  readonly importing = signal<boolean>(false);
  readonly validationErrors = signal<string[]>([]);

  // Computed
  readonly hasPreview = computed(() => this.previewData().length > 0);
  
  readonly canProceed = computed(() => {
    return this.selectedVendor() !== null
        && this.selectedFile() !== null
        && this.selectedHeaderRow() !== null;
  });

  readonly extractedColumns = computed(() => {
    const data = this.previewData();
    const headerRowIdx = this.selectedHeaderRow();
    if (headerRowIdx === null || !data[headerRowIdx]) return [];
    
    const headers = data[headerRowIdx].map(col => col?.trim() || '');
    const seen = new Set<string>();
    
    return headers
      .filter(h => h.length > 0) // Remove empty headers
      .filter(h => {
        if (seen.has(h)) return false; // Remove duplicates
        seen.add(h);
        return true;
      });
  });

  readonly isMappingValid = computed(() => {
    const currentMappings = this.mappings();
    
    // Check if all required fields are mapped
    for (const field of IMPORT_SYSTEM_FIELDS) {
      if (field.required) {
        const mapped = currentMappings.find(m => m.systemField === field.id && !!m.excelColumn);
        if (!mapped) return false;
      }
    }
    
    // Also, checking for duplicate Excel columns in mappings
    const excelColumns = currentMappings.map(m => m.excelColumn).filter(h => !!h);
    const uniqueHeaders = new Set(excelColumns);
    if (excelColumns.length !== uniqueHeaders.size) return false;

    return true;
  });

  readonly canGoToConfirmation = computed(() => {
    return this.canProceed() && this.isMappingValid();
  });

  // Actions
  replaceFile(file: File) {
    this.selectedFile.set(file);
    this.messageService.add({ severity: 'success', summary: 'Success', detail: 'File replaced.' });
    this.resetPreviewAndMappings();
  }

  removeFile() {
    this.selectedFile.set(null);
    this.resetPreviewAndMappings();
  }

  private resetPreviewAndMappings() {
    this.previewData.set([]);
    this.totalRowsFound.set(0);
    this.selectedHeaderRow.set(null);
    this.initialMappings.set([]);
    this.mappings.set([]);
  }

  setVendor(vendorId: string | null) {
    this.selectedVendor.set(vendorId);
    this.initialMappings.set([]);
    this.mappings.set([]);
  }

  setPreviewData(dataGrid: string[][], totalRows: number) {
    this.previewData.set(dataGrid);
    this.totalRowsFound.set(totalRows);
  }

  setSelectedHeaderRow(index: number | null) {
    this.selectedHeaderRow.set(index);
    this.mappings.set([...this.initialMappings()]); // Reset to initial on row change
  }

  setMappings(mappings: ColumnMapping[]) {
    this.mappings.set(mappings);
  }
  
  setInitialMappings(mappings: ColumnMapping[]) {
    this.initialMappings.set(mappings);
    this.mappings.set([...mappings]);
  }

  setPreviewLoading(loading: boolean) {
    this.previewLoading.set(loading);
  }

  setMappingLoading(loading: boolean) {
    this.mappingLoading.set(loading);
  }

  async loadVendorMappings(vendorId: string) {
    this.setMappingLoading(true);
    try {
      const response = await firstValueFrom(this.api.getVendorMappings(vendorId));
      if (response.success && response.data) {
        const mappings: ColumnMapping[] = response.data.map(m => ({
          systemField: m.systemField,
          excelColumn: m.excelColumn,
          isCustom: false
        }));
        this.setInitialMappings(mappings);
      }
    } catch (error) {
      this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Vendor mappings could not be loaded.' });
    } finally {
      this.setMappingLoading(false);
    }
  }

  async importPurchaseOrder() {
    const vendorId = this.selectedVendor();
    const file = this.selectedFile();

    if (!vendorId || !file) {
      this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Import failed: Vendor and File are required.' });
      return;
    }

    if (!this.isMappingValid()) {
      this.messageService.add({ severity: 'warn', summary: 'Warning', detail: 'Required mappings missing or duplicate columns exist.' });
      return;
    }

    this.importing.set(true);
    
    try {
      // 1. Save Mappings Transaction
      const current = this.mappings();
      const initial = this.initialMappings();

      const changedMappings: VendorMapping[] = current
        .filter(m => {
          const initM = initial.find(im => im.systemField === m.systemField);
          return !initM || initM.excelColumn !== m.excelColumn;
        })
        .map(m => ({
          vendorId,
          systemField: m.systemField,
          excelColumn: m.excelColumn
        }));

      if (changedMappings.length > 0) {
        this.savingMappings.set(true);
        try {
          await firstValueFrom(this.api.saveVendorMappings(changedMappings));
          // Update initial mappings after save to reflect current state
          this.initialMappings.set([...current]); 
        } catch (error) {
          this.messageService.add({ 
            severity: 'error', 
            summary: 'Unable to save vendor mappings', 
            detail: 'Purchase Order import was cancelled because one or more vendor mappings could not be saved.' 
          });
          return; // Abort import pipeline
        } finally {
          this.savingMappings.set(false);
        }
      }

      // 1.5 Verify Mappings Transaction
      try {
        const verificationResponse = await firstValueFrom(this.api.getVendorMappings(vendorId));
        if (!verificationResponse.success || !verificationResponse.data) {
          throw new Error('Verification failed');
        }
        
        // Check if all required mappings are present in the DB
        const dbMappings = verificationResponse.data;
        for (const field of IMPORT_SYSTEM_FIELDS) {
          if (field.required) {
            const mapped = dbMappings.find(m => m.systemField === field.id && !!m.excelColumn);
            if (!mapped) {
              throw new Error(`Required mapping ${field.label} missing in DB`);
            }
          }
        }
      } catch (error) {
        this.messageService.add({ 
          severity: 'error', 
          summary: 'Unable to verify vendor mappings.', 
          detail: 'Verification failed. Cannot proceed with import.' 
        });
        return; // Abort import pipeline
      }

      // 2. Import Transaction
      const formData = new FormData();
      formData.append('vendorId', vendorId);
      formData.append('file', file);
      formData.append('hasMixedVatRates', 'false'); // Future configurable option

      const response = await firstValueFrom(this.api.importPurchaseOrder(formData));
      
      if (response.success && response.data) {
        const result = response.data;
        const msg = `Items Imported: ${result.itemsImported}\nProducts Created: ${result.productsCreated}\nProducts Matched: ${result.productsMatched}\nRows Skipped: ${result.rowsSkipped}`;
        
        this.messageService.add({ 
          severity: result.rowsSkipped > 0 ? 'warn' : 'success', 
          summary: 'Purchase Order imported successfully.', 
          detail: msg,
          life: 8000
        });
        this.listStore.invalidateCache();
        await this.listStore.loadPurchaseOrders();
        this.reset(); // Clear wizard state
        this.router.navigate(['/dashboard/purchase-orders']);
      } else {
        this.messageService.add({ severity: 'error', summary: 'Error', detail: response.message || 'Import failed.' });
      }
    } catch (error) {
      this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Import failed.' });
    } finally {
      this.importing.set(false);
    }
  }

  reset() {
    this.selectedFile.set(null);
    this.selectedVendor.set(null);
    this.resetPreviewAndMappings();
    this.validationErrors.set([]);
  }
}
