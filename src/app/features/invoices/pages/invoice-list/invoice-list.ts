import { Component, inject, OnInit, signal, ElementRef, ViewChild, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';

import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { TooltipModule } from 'primeng/tooltip';
import { DialogModule } from 'primeng/dialog';
import { TagModule } from 'primeng/tag';
import { AutoCompleteModule, AutoCompleteCompleteEvent } from 'primeng/autocomplete';
import { SelectModule } from 'primeng/select';

import { PaginationComponent } from '../../../../shared/table/components/pagination/pagination';
import { EmptyStateComponent } from '../../../../shared/table/components/empty-state/empty-state';
import { LoadingSkeletonComponent } from '../../../../shared/table/components/loading-skeleton/loading-skeleton';
import { ErrorStateComponent } from '../../../../shared/table/components/error-state/error-state';
import { ToolbarComponent } from '../../../../shared/table/components/toolbar/toolbar';

import { InvoiceFacade } from '../../facades/invoice.facade';
import { INVOICE_TABLE_COLUMNS } from '../../constants/invoice-table-columns.constant';
import { InvoiceListItemDto } from '../../models/invoice.model';
import { PurchaseOrderLookupService } from '../../../../shared/lookups/services/purchase-order-lookup.service';
import { VendorLookupService } from '../../../../shared/lookups/services/vendor-lookup.service';
import { PurchaseOrder, PurchaseOrderStatus } from '../../../../features/purchase-orders/models/purchase-order.model';
import { InvoiceUploadContextService } from '../../services/invoice-upload-context.service';

/** Allowed file types for invoice upload */
const ALLOWED_EXTENSIONS = ['.pdf', '.xml', '.zip'];
const ALLOWED_MIME_TYPES = [
  'application/pdf',
  'text/xml',
  'application/xml',
  'application/zip',
  'application/x-zip-compressed'
];
const MAX_FILE_SIZE_BYTES = 20 * 1024 * 1024; // 20 MB

@Component({
  selector: 'app-invoice-list',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    TableModule,
    ButtonModule,
    TooltipModule,
    DialogModule,
    TagModule,
    AutoCompleteModule,
    SelectModule,
    PaginationComponent,
    EmptyStateComponent,
    ErrorStateComponent,
    LoadingSkeletonComponent,
    ToolbarComponent
  ],
  templateUrl: './invoice-list.html'
})
export class InvoiceListComponent implements OnInit {
  readonly facade    = inject(InvoiceFacade);
  readonly poLookup  = inject(PurchaseOrderLookupService);
  readonly vendorLookup = inject(VendorLookupService);
  private readonly router = inject(Router);
  private readonly uploadContextService = inject(InvoiceUploadContextService);

  /** Reference to the Upload button for focus restoration after dialog close */
  @ViewChild('uploadBtn') uploadBtn?: ElementRef<HTMLButtonElement>;

  readonly columns = INVOICE_TABLE_COLUMNS;

  // ── Upload dialog state ─────────────────────────────────────────────
  readonly contextPo             = signal<PurchaseOrder | null>(null);
  readonly uploadDialogVisible   = signal<boolean>(false);
  readonly selectedPurchaseOrder = signal<PurchaseOrder | null>(null);
  readonly selectedFile          = signal<File | null>(null);
  readonly fileError             = signal<string | null>(null);
  readonly isDragOver            = signal<boolean>(false);
  /** Show validation messages only after first submit attempt */
  readonly submitted             = signal<boolean>(false);

  // ── Filter Options ─────────────────────────────────────────────
  readonly invoiceStatusOptions = [
    { label: 'Pending', value: 'Pending' },
    { label: 'Processing', value: 'Processing' },
    { label: 'Matched', value: 'Matched' },
    { label: 'Completed', value: 'Completed' },
    { label: 'Failed', value: 'Failed' },
  ];

  readonly discrepancyOptions = [
    { label: 'All', value: null },
    { label: 'Has Discrepancies', value: true },
    { label: 'No Discrepancies', value: false }
  ];

  readonly poFilterOptions = computed(() => {
    const searchResults = this.poLookup.suggestions() || [];
    const selectedId = this.facade.selectedPurchaseOrderId();
    
    if (!selectedId) {
      return searchResults;
    }
    
    if (searchResults.some(po => po.id === selectedId.toString())) {
      return searchResults;
    }
    
    const pos = this.facade.rawInvoices();
    const invoiceWithSelectedPo = pos.find(inv => inv.purchaseOrderId === selectedId);
    
    if (invoiceWithSelectedPo) {
      const retainedPo = {
        id: selectedId.toString(),
        orderNumber: invoiceWithSelectedPo.purchaseOrderNumber ?? ''
      } as unknown as PurchaseOrder;
      return [retainedPo, ...searchResults];
    }
    
    return searchResults;
  });

  readonly vendorFilterOptions = computed(() => {
    const searchResults = this.vendorLookup.vendors() || [];
    const selectedName = this.facade.selectedVendorName();
    
    if (!selectedName) {
      return searchResults;
    }
    
    if (searchResults.some(v => v.name === selectedName)) {
      return searchResults;
    }
    
    // We only need the name for Invoices table filtering
    const retainedVendor = {
      id: '',
      name: selectedName,
      erpId: ''
    };
    return [retainedVendor, ...searchResults];
  });

  // ── Validation helpers ──────────────────────────────────────────────
  get poInvalid():   boolean { return this.submitted() && !this.selectedPurchaseOrder(); }
  get fileInvalid(): boolean { return this.submitted() && !this.selectedFile(); }

  get canSubmitUpload(): boolean {
    return (
      !!this.selectedPurchaseOrder() &&
      !!this.selectedFile() &&
      !this.facade.uploadLoading()
    );
  }

  // ── Lifecycle ───────────────────────────────────────────────────────
  ngOnInit(): void {
    this.facade.loadInvoices();
    this.vendorLookup.search('');
    this.poLookup.search('');

    const po = this.uploadContextService.consumePurchaseOrder();
    if (po) {
      this.contextPo.set(po);
      this.selectedPurchaseOrder.set(po);
      this.uploadDialogVisible.set(true);
    }
  }

  // ── Dropdown Handlers ───────────────────────────────────────────────
  onVendorFilter(event: any) {
    this.vendorLookup.search(event.filter);
  }

  onVendorLazyLoad(event: any) {
    const currentOptionsCount = this.vendorLookup.vendors().length;
    if (event.last >= currentOptionsCount && this.vendorLookup.hasMore()) {
      this.vendorLookup.loadMore();
    }
  }

  onPOFilter(event: any) {
    this.poLookup.search(event.filter);
  }

  onPOLazyLoad(event: any) {
    const currentOptionsCount = this.poLookup.suggestions().length;
    if (event.last >= currentOptionsCount && this.poLookup.hasMore()) {
      this.poLookup.loadMore();
    }
  }

  // ── Upload dialog ───────────────────────────────────────────────────
  openUploadDialog(): void {
    this._resetUploadForm();
    this.uploadDialogVisible.set(true);
    this.poLookup.search('');
  }

  closeUploadDialog(): void {
    this.uploadDialogVisible.set(false);
    this._resetUploadForm();
    this.contextPo.set(null);
    setTimeout(() => {
      const btn = document.getElementById('uploadInvoiceBtn') as HTMLButtonElement | null;
      btn?.focus();
    }, 50);
  }

  // ── PO Autocomplete ─────────────────────────────────────────────────
  searchPurchaseOrders(event: AutoCompleteCompleteEvent): void {
    this.poLookup.search(event.query);
  }

  onPOSelected(po: PurchaseOrder): void {
    this.selectedPurchaseOrder.set(po);
  }

  onPOCleared(): void {
    this.selectedPurchaseOrder.set(null);
  }

  // ── File handling ───────────────────────────────────────────────────
  onFileSelect(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      this._processFile(input.files[0]);
    }
    input.value = '';
  }

  onDragOver(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragOver.set(true);
  }

  onDragLeave(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragOver.set(false);
  }

  onDrop(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragOver.set(false);
    const file = event.dataTransfer?.files[0];
    if (file) this._processFile(file);
  }

  removeFile(): void {
    this.selectedFile.set(null);
    this.fileError.set(null);
  }

  submitUpload(): void {
    this.submitted.set(true);
    const po   = this.selectedPurchaseOrder();
    const file = this.selectedFile();
    if (!po || !file) return;

    this.facade.uploadInvoice(po.id, file, () => {
      this.closeUploadDialog();
    });
  }

  // ── Table actions ───────────────────────────────────────────────────
  downloadInvoice(invoice: InvoiceListItemDto): void {
    this.facade.downloadInvoice(invoice.id);
  }

  isDownloading(id: number): boolean {
    return this.facade.downloadingId() === id;
  }

  viewDetail(invoice: InvoiceListItemDto): void {
    this.router.navigate(['/dashboard/invoices', invoice.id]);
  }

  // ── Display helpers — Invoice status ────────────────────────────────
  getStatusSeverity(status: string | null): 'success' | 'info' | 'warn' | 'danger' | 'secondary' | 'contrast' {
    switch ((status ?? '').toLowerCase()) {
      case 'completed':  return 'success';
      case 'matched':    return 'success';
      case 'processing': return 'info';
      case 'pending':    return 'warn';
      case 'failed':     return 'danger';
      default:           return 'secondary';
    }
  }

  // ── Display helpers — PO status ──────────────────────────────────────
  getPOStatusLabel(status: PurchaseOrderStatus): string {
    const labels: Partial<Record<PurchaseOrderStatus, string>> = {
      [PurchaseOrderStatus.Draft]:           'Draft',
      [PurchaseOrderStatus.PendingApproval]: 'Pending Approval',
      [PurchaseOrderStatus.Approved]:        'Approved',
      [PurchaseOrderStatus.Rejected]:        'Rejected',
      [PurchaseOrderStatus.Fulfilled]:       'Fulfilled',
      [PurchaseOrderStatus.Cancelled]:       'Cancelled'
    };
    return labels[status] ?? 'Unknown';
  }

  getPOStatusSeverity(status: PurchaseOrderStatus): 'success' | 'info' | 'warn' | 'danger' | 'secondary' | 'contrast' {
    switch (status) {
      case PurchaseOrderStatus.Approved:        return 'success';
      case PurchaseOrderStatus.Draft:           return 'secondary';
      case PurchaseOrderStatus.PendingApproval: return 'warn';
      case PurchaseOrderStatus.Rejected:        return 'danger';
      case PurchaseOrderStatus.Fulfilled:       return 'info';
      case PurchaseOrderStatus.Cancelled:       return 'danger';
      default:                                  return 'secondary';
    }
  }

  // ── Display helpers — Formatting ─────────────────────────────────────
  formatAmount(amount: number): string {
    return new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(amount);
  }

  formatDate(dateStr: string | null | undefined): string {
    if (!dateStr) return '—';
    return new Date(dateStr).toLocaleDateString('en-US', {
      year: 'numeric', month: 'short', day: '2-digit'
    });
  }

  getFileIcon(filename: string | null): string {
    if (!filename) return 'pi pi-file';
    const ext = filename.toLowerCase().split('.').pop();
    if (ext === 'pdf') return 'pi pi-file-pdf';
    if (ext === 'xml') return 'pi pi-file-edit';
    if (ext === 'zip') return 'pi pi-file-arrow-up';
    return 'pi pi-file';
  }

  formatFileSize(bytes: number): string {
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
  }

  goToPurchaseOrders(): void {
    this.closeUploadDialog();
    this.router.navigate(['/dashboard/purchase-orders']);
  }

  // ── Private helpers ──────────────────────────────────────────────────
  private _processFile(file: File): void {
    this.fileError.set(null);

    const ext = '.' + file.name.toLowerCase().split('.').pop();
    if (!ALLOWED_EXTENSIONS.includes(ext)) {
      this.fileError.set(`Only PDF, XML, and ZIP files are accepted. Received: ${ext}`);
      return;
    }

    if (file.type !== '' && !ALLOWED_MIME_TYPES.includes(file.type)) {
      this.fileError.set(`Invalid file type: ${file.type}`);
      return;
    }

    if (file.size > MAX_FILE_SIZE_BYTES) {
      this.fileError.set(`File exceeds the maximum allowed size of 20 MB`);
      return;
    }

    this.selectedFile.set(file);
    this.fileError.set(null);
  }

  private _resetUploadForm(): void {
    this.selectedPurchaseOrder.set(null);
    this.selectedFile.set(null);
    this.fileError.set(null);
    this.isDragOver.set(false);
    this.submitted.set(false);
    this.poLookup.clear();
  }
}
