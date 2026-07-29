import { Injectable, inject, signal } from '@angular/core';
import { InvoiceService } from '../services/invoice.service';
import { InvoiceStore } from '../stores/invoice.store';
import { AuthService } from '../../../core/auth/services/auth.service';
import { PERMISSIONS } from '../../../core/auth/constants/permissions';
import { MessageService } from 'primeng/api';

@Injectable()
export class InvoiceFacade {
  // --- Table state (delegated from store) ---
  readonly state;
  readonly items;
  readonly totalCount;
  readonly pageNumber;
  readonly pageSize;
  readonly loading;
  /** Search term is preserved in the store for future backend support, but the UI hides the input. */
  readonly searchTerm;

  // --- Permission signals (computed once in the facade; templates must not call authService directly) ---
  readonly canUpload;
  readonly canDownload;
  readonly canViewAll;

  // --- Upload state ---
  readonly uploadLoading = signal<boolean>(false);

  // --- Per-row download state ---
  /** ID of the invoice currently being downloaded, or null. Used for row-level loading indicator. */
  readonly downloadingId = signal<number | null>(null);

  constructor(
    private readonly service: InvoiceService,
    private readonly store: InvoiceStore,
    private readonly authService: AuthService,
    private readonly messageService: MessageService
  ) {
    this.state        = this.store.table.state;
    this.items        = this.store.table.items;
    this.totalCount   = this.store.table.totalCount;
    this.pageNumber   = this.store.table.pageNumber;
    this.pageSize     = this.store.table.pageSize;
    this.loading      = this.store.table.loading;
    this.searchTerm   = this.store.table.searchTerm;

    this.canUpload    = this.authService.hasPermission(PERMISSIONS.invoices.upload);
    this.canDownload  = this.authService.hasPermission(PERMISSIONS.invoices.download);
    this.canViewAll   = this.authService.hasPermission(PERMISSIONS.invoices.viewAll);
  }

  // --- Data loading ---

  loadInvoices(): void {
    if (this.loading()) return;
    this.store.table.setLoading(true);
    this.service
      .getInvoices(this.pageNumber(), this.pageSize())
      .subscribe({
        next: response => {
          if (response.success && response.data) {
            this.store.table.setItems(response.data.items ?? [], response.data.totalCount);
          } else {
            this.store.table.setError(response.message ?? 'Failed to load invoices');
          }
          this.store.table.setLoading(false);
        },
        error: () => {
          this.store.table.setError('An error occurred while loading invoices');
          this.store.table.setLoading(false);
        }
      });
  }

  setPage(pageNumber: number, pageSize: number): void {
    this.store.table.setPage(pageNumber, pageSize);
    this.loadInvoices();
  }

  refresh(): void {
    this.loadInvoices();
  }

  // --- Upload ---

  /**
   * Upload a new invoice file linked to a Purchase Order.
   * On success: closes dialog (via callback), resets form, and reloads the current page.
   */
  uploadInvoice(purchaseOrderId: string, file: File, onSuccess: () => void): void {
    if (this.uploadLoading()) return;
    this.uploadLoading.set(true);
    this.service.uploadInvoice(purchaseOrderId, file).subscribe({
      next: response => {
        if (response.success) {
          this.messageService.add({
            severity: 'success',
            summary: 'Invoice Uploaded',
            detail: `Invoice uploaded successfully.`
          });
          onSuccess();
          this.loadInvoices();
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

  /**
   * Download invoice file by ID.
   * Reads Content-Disposition header for filename; fallback: invoice-{id}.pdf
   */
  downloadInvoice(id: number): void {
    if (this.downloadingId() !== null) return; // prevent concurrent downloads
    this.downloadingId.set(id);

    this.service.downloadInvoiceWithHeaders(id).subscribe({
      next: response => {
        const blob = response.body;
        if (!blob) {
          this.messageService.add({ severity: 'error', summary: 'Download Failed', detail: 'No content received' });
          this.downloadingId.set(null);
          return;
        }

        // Resolve filename from Content-Disposition or fallback
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
    // Try filename*= (RFC 5987) first, then filename=
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
