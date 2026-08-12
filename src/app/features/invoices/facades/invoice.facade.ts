import { Injectable, computed, inject, signal } from '@angular/core';
import { InvoiceService } from '../services/invoice.service';
import { InvoiceStore } from '../stores/invoice.store';
import { AuthService } from '../../../core/auth/services/auth.service';
import { PERMISSIONS } from '../../../core/auth/constants/permissions';
import { MessageService } from 'primeng/api';
import { InvoiceListItemDto } from '../models/invoice.model';
import { firstValueFrom } from 'rxjs';

const ALL_ITEMS_PAGE_SIZE = 1000;

@Injectable()
export class InvoiceFacade {
  // --- Raw dataset ---
  readonly rawInvoices = signal<InvoiceListItemDto[]>([]);

  // --- Filter State ---
  readonly searchTerm = signal<string>('');
  readonly selectedStatus = signal<string | null>(null);
  readonly selectedVendorName = signal<string | null>(null);
  readonly selectedPurchaseOrderId = signal<number | null>(null);
  readonly hasDiscrepancies = signal<boolean | null>(null);

  // --- Pagination State ---
  readonly pageNumber = signal<number>(1);
  readonly pageSize = signal<number>(10);

  // --- UI State ---
  readonly loading = signal<boolean>(false);
  readonly error = signal<string | null>(null);

  // --- Permission signals ---
  readonly canUpload: boolean;
  readonly canDownload: boolean;
  readonly canViewAll: boolean;

  // --- Upload & Download state ---
  readonly uploadLoading = signal<boolean>(false);
  readonly downloadingId = signal<number | null>(null);

  // --- Computed: Filtered Data ---
  readonly filteredInvoices = computed(() => {
    const search = this.searchTerm().trim().toLowerCase();
    const status = this.selectedStatus();
    const vendorName = this.selectedVendorName()?.trim().toLowerCase();
    const purchaseOrderId = this.selectedPurchaseOrderId();
    const discrepancy = this.hasDiscrepancies();

    return this.rawInvoices().filter(invoice => {
      const matchesSearch = !search ||
        invoice.invoiceNumber?.toLowerCase().includes(search) ||
        invoice.vendorName?.toLowerCase().includes(search) ||
        invoice.purchaseOrderNumber?.toLowerCase().includes(search);

      const matchesStatus = !status || invoice.status === status;
      
      /**
       * InvoiceListItemDto currently exposes vendorName but not vendorId.
       * Vendor filtering therefore uses the normalized vendor name.
       *
       * If the backend later exposes vendorId, replace this filter with
       * ID-based comparison.
       */
      const invoiceVendorName = invoice.vendorName?.trim().toLowerCase();
      const matchesVendor = !vendorName || invoiceVendorName === vendorName;
      
      const matchesPO = purchaseOrderId === null || invoice.purchaseOrderId === purchaseOrderId;
      const matchesDiscrepancy = discrepancy === null || invoice.hasDiscrepancies === discrepancy;

      return matchesSearch && matchesStatus && matchesVendor && matchesPO && matchesDiscrepancy;
    });
  });

  // --- Computed: Paginated Data ---
  // We name it items to maintain compatibility with the existing template
  readonly items = computed(() => {
    const all = this.filteredInvoices();
    const start = (this.pageNumber() - 1) * this.pageSize();
    return all.slice(start, start + this.pageSize());
  });

  readonly totalCount = computed(() => this.filteredInvoices().length);

  readonly state = computed(() => {
    if (this.loading()) return 'loading';
    if (this.error()) return 'error';
    if (this.filteredInvoices().length === 0) return 'empty';
    return 'data';
  });

  constructor(
    private readonly service: InvoiceService,
    private readonly store: InvoiceStore,
    private readonly authService: AuthService,
    private readonly messageService: MessageService
  ) {
    this.canUpload = this.authService.hasPermission(PERMISSIONS.invoices.upload);
    this.canDownload = this.authService.hasPermission(PERMISSIONS.invoices.download);
    this.canViewAll = this.authService.hasPermission(PERMISSIONS.invoices.viewAll);
  }

  // --- Data loading ---

  async loadInvoices() {
    if (this.loading()) return;
    this.loading.set(true);
    this.error.set(null);

    try {
      let currentPage = 1;
      let hasNextPage = true;
      const allItems: InvoiceListItemDto[] = [];

      while (hasNextPage) {
        const response = await firstValueFrom(this.service.getInvoices(currentPage, ALL_ITEMS_PAGE_SIZE));
        
        if (response.success && response.data) {
          allItems.push(...(response.data.items ?? []));
          hasNextPage = response.data.hasNextPage;
          currentPage++;
        } else {
          this.error.set(response.message ?? 'Failed to load invoices');
          this.messageService.add({ severity: 'error', summary: 'Error', detail: this.error()! });
          break;
        }
      }

      if (!this.error()) {
        this.rawInvoices.set(allItems);
      }
    } catch (err) {
      this.error.set('An error occurred while loading invoices');
      this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Failed to load invoices' });
    } finally {
      this.loading.set(false);
    }
  }

  // --- Filter Setters ---
  setSearchTerm(term: string): void {
    this.searchTerm.set(term);
    this.pageNumber.set(1);
  }

  setStatus(status: string | null): void {
    this.selectedStatus.set(status);
    this.pageNumber.set(1);
  }

  setVendorName(name: string | null): void {
    this.selectedVendorName.set(name);
    this.pageNumber.set(1);
  }

  setPurchaseOrder(id: number | null): void {
    this.selectedPurchaseOrderId.set(id);
    this.pageNumber.set(1);
  }

  setHasDiscrepancies(value: boolean | null): void {
    this.hasDiscrepancies.set(value);
    this.pageNumber.set(1);
  }

  setPage(pageNumber: number, pageSize: number): void {
    this.pageNumber.set(pageNumber);
    this.pageSize.set(pageSize);
  }

  clearFilters(): void {
    this.searchTerm.set('');
    this.selectedStatus.set(null);
    this.selectedVendorName.set(null);
    this.selectedPurchaseOrderId.set(null);
    this.hasDiscrepancies.set(null);
    this.pageNumber.set(1);
  }

  refresh(): void {
    this.loadInvoices();
  }

  // --- Upload ---
  uploadInvoice(purchaseOrderId: string, file: File, onSuccess: (invoiceId: number) => void): void {
    if (this.uploadLoading()) return;
    this.uploadLoading.set(true);
    this.service.uploadInvoice(purchaseOrderId, file).subscribe({
      next: response => {
        if (response.success && response.data) {
          this.messageService.add({
            severity: 'success',
            summary: 'Invoice Uploaded',
            detail: `Invoice uploaded successfully.`
          });
          onSuccess(response.data.invoiceId);
          this.loadInvoices(); // Refresh from backend
        } else {
          this.messageService.add({
            severity: 'error',
            summary: 'Upload Failed',
            detail: response.message ?? 'Failed to upload invoice'
          });
        }
        this.uploadLoading.set(false);
      },
      error: err => {
        const detail =
          err.status === 400
            ? err.error?.message ?? 'Invalid file or Purchase Order ID'
            : 'An error occurred during upload';
        this.messageService.add({ severity: 'error', summary: 'Upload Failed', detail });
        this.uploadLoading.set(false);
      }
    });
  }

  // --- Download ---
  downloadInvoice(id: number): void {
    if (this.downloadingId() !== null) return;
    this.downloadingId.set(id);

    this.service.downloadInvoiceWithHeaders(id).subscribe({
      next: response => {
        const blob = response.body;
        if (!blob) {
          this.messageService.add({ severity: 'error', summary: 'Download Failed', detail: 'No content received' });
          this.downloadingId.set(null);
          return;
        }

        const disposition = response.headers.get('Content-Disposition') ?? '';
        const filename = this._extractFilename(disposition) ?? `invoice-${id}.pdf`;

        this._triggerBlobDownload(blob, filename);
        this.downloadingId.set(null);
      },
      error: () => {
        this.messageService.add({
          severity: 'error',
          summary: 'Download Failed',
          detail: 'Could not download the invoice file'
        });
        this.downloadingId.set(null);
      }
    });
  }

  // --- Private helpers ---
  private _extractFilename(disposition: string): string | null {
    const utf8Match = disposition.match(/filename\*=UTF-8''([^;\n]+)/i);
    if (utf8Match) {
      return decodeURIComponent(utf8Match[1].trim());
    }
    const simpleMatch = disposition.match(/filename="?([^";\n]+)"?/i);
    if (simpleMatch) {
      return simpleMatch[1].trim();
    }
    return null;
  }

  private _triggerBlobDownload(blob: Blob, filename: string): void {
    const url = URL.createObjectURL(blob);
    const anchor = document.createElement('a');
    anchor.href = url;
    anchor.download = filename;
    anchor.style.display = 'none';
    document.body.appendChild(anchor);
    anchor.click();
    document.body.removeChild(anchor);
    URL.revokeObjectURL(url);
  }
}
