import { Component, inject, OnInit, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { PurchaseOrderStoreService } from '../../services/purchase-order-store.service';
import { AuthService } from '../../../../core/auth/services/auth.service';
import { PERMISSIONS } from '../../../../core/auth/constants/permissions';
import { PurchaseOrder, PurchaseOrderStatus } from '../../models/purchase-order.model';
import { VendorLookupService } from '../../../../shared/lookups/services/vendor-lookup.service';

import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { TagModule } from 'primeng/tag';
import { TooltipModule } from 'primeng/tooltip';
import { SelectModule } from 'primeng/select';

import { ToolbarComponent } from '../../../../shared/table/components/toolbar/toolbar';
import { PaginationComponent } from '../../../../shared/table/components/pagination/pagination';
import { EmptyStateComponent } from '../../../../shared/table/components/empty-state/empty-state';
import { LoadingSkeletonComponent } from '../../../../shared/table/components/loading-skeleton/loading-skeleton';
import { ErrorStateComponent } from '../../../../shared/table/components/error-state/error-state';
import { DeleteConfirmationComponent } from '../../../../shared/dialogs/components/delete-confirmation/delete-confirmation';

import { InvoiceUploadContextService } from '../../../invoices/services/invoice-upload-context.service';
import { VendorOption } from '../../../../shared/lookups/models/vendor-option.model';

@Component({
  selector: 'app-purchase-order-list',
  standalone: true,
  imports: [
    CommonModule, 
    FormsModule,
    TableModule, 
    ButtonModule, 
    TagModule,
    TooltipModule,
    SelectModule,
    ToolbarComponent,
    PaginationComponent,
    EmptyStateComponent,
    ErrorStateComponent,
    LoadingSkeletonComponent,
    DeleteConfirmationComponent
  ],
  templateUrl: './purchase-order-list.component.html'
})
export class PurchaseOrderListComponent implements OnInit {
  store = inject(PurchaseOrderStoreService);
  authService = inject(AuthService);
  router = inject(Router);
  uploadContextService = inject(InvoiceUploadContextService);
  vendorLookup = inject(VendorLookupService);

  columns = [
    { field: 'orderNumber', header: 'Order Number' },
    { field: 'vendorName', header: 'Vendor' },
    { field: 'status', header: 'Status' },
    { field: 'orderDate', header: 'Order Date' },
    { field: 'totalAmount', header: 'Total Amount' },
    { field: 'requestedBy', header: 'Requested By' }
  ];
  
  statusOptions = [
    { label: 'Draft', value: PurchaseOrderStatus.Draft },
    { label: 'Pending Approval', value: PurchaseOrderStatus.PendingApproval },
    { label: 'Approved', value: PurchaseOrderStatus.Approved },
    { label: 'Rejected', value: PurchaseOrderStatus.Rejected },
    { label: 'Fulfilled', value: PurchaseOrderStatus.Fulfilled },
    { label: 'Cancelled', value: PurchaseOrderStatus.Cancelled }
  ];

  canImport = this.authService.hasPermission(PERMISSIONS.poImports.import);
  canViewDetails = this.authService.hasPermission(PERMISSIONS.poImports.view);
  canDelete = this.authService.hasPermission(PERMISSIONS.poImports.delete);
  canUploadInvoice = this.authService.hasPermission(PERMISSIONS.invoices.upload);

  // Modal State
  deleteDialogVisible = false;
  selectedPoIdToDelete: string | null = null;
  purchaseOrderToDelete: string | null = null;

  // Computed options to ensure the selected vendor remains visible if it drops out of search results
  vendorOptions = computed(() => {
    const searchResults = this.vendorLookup.vendors() || [];
    const selectedId = this.store.selectedVendorId();
    
    // If no selection, just return the search results
    if (!selectedId) {
      return searchResults;
    }
    
    // Check if the selected vendor is in the search results
    const isSelectedInResults = searchResults.some(v => v.id === selectedId);
    
    if (isSelectedInResults) {
      return searchResults;
    }
    
    // We need to find the selected vendor's name from our raw POs to display it properly
    const pos = this.store.rawPurchaseOrders();
    const poWithSelectedVendor = pos.find(po => po.vendorId === selectedId);
    
    if (poWithSelectedVendor) {
      const retainedVendor: VendorOption = {
        id: selectedId,
        name: poWithSelectedVendor.vendorName,
        erpId: ''
      };
      return [retainedVendor, ...searchResults];
    }
    
    return searchResults;
  });

  constructor() {
    this.store.loadPurchaseOrders();
  }

  ngOnInit() {
    this.vendorLookup.search('');
  }
  
  onVendorFilter(event: any) {
    this.vendorLookup.search(event.filter);
  }

  onVendorLazyLoad(event: any) {
    const currentOptionsCount = this.vendorLookup.vendors().length;
    if (event.last >= currentOptionsCount && this.vendorLookup.hasMore()) {
      this.vendorLookup.loadMore();
    }
  }

  onImport() {
    this.router.navigate(['/dashboard/purchase-orders/import']);
  }

  onViewDetails(id: string) {
    this.router.navigate(['/dashboard/purchase-orders', id]);
  }

  onUploadInvoice(po: PurchaseOrder) {
    this.uploadContextService.setPurchaseOrder(po);
    this.router.navigate(['/dashboard/invoices']);
  }

  confirmDelete(id: string) {
    this.purchaseOrderToDelete = id;
    this.deleteDialogVisible = true;
  }

  onDelete() {
    if (this.purchaseOrderToDelete) {
      this.store.deletePurchaseOrder(this.purchaseOrderToDelete, () => {
        this.deleteDialogVisible = false;
        this.purchaseOrderToDelete = null;
      });
    }
  }

  getStatusLabel(status: PurchaseOrderStatus | undefined): string {
    switch (status) {
      case PurchaseOrderStatus.Draft: return 'Draft';
      case PurchaseOrderStatus.PendingApproval: return 'Pending Approval';
      case PurchaseOrderStatus.Approved: return 'Approved';
      case PurchaseOrderStatus.Rejected: return 'Rejected';
      case PurchaseOrderStatus.Fulfilled: return 'Fulfilled';
      case PurchaseOrderStatus.Cancelled: return 'Cancelled';
      default: return 'Unknown';
    }
  }

  getStatusSeverity(status: PurchaseOrderStatus | undefined): 'success' | 'info' | 'warn' | 'danger' | 'secondary' | 'contrast' | undefined {
    switch (status) {
      case PurchaseOrderStatus.Draft: return 'secondary';
      case PurchaseOrderStatus.PendingApproval: return 'warn';
      case PurchaseOrderStatus.Approved: return 'success';
      case PurchaseOrderStatus.Rejected: return 'danger';
      case PurchaseOrderStatus.Fulfilled: return 'info';
      case PurchaseOrderStatus.Cancelled: return 'secondary';
      default: return 'secondary';
    }
  }
}
