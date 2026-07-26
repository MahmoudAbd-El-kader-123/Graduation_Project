import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { PurchaseOrderStoreService } from '../../services/purchase-order-store.service';
import { AuthService } from '../../../../core/auth/services/auth.service';
import { PERMISSIONS } from '../../../../core/auth/constants/permissions';
import { PurchaseOrder, PurchaseOrderStatus } from '../../models/purchase-order.model';

import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { TagModule } from 'primeng/tag';
import { TooltipModule } from 'primeng/tooltip';
import { ConfirmDialogModule } from 'primeng/confirmdialog';

import { ToolbarComponent } from '../../../../shared/table/components/toolbar/toolbar';
import { PaginationComponent } from '../../../../shared/table/components/pagination/pagination';
import { EmptyStateComponent } from '../../../../shared/table/components/empty-state/empty-state';
import { LoadingSkeletonComponent } from '../../../../shared/table/components/loading-skeleton/loading-skeleton';
import { ErrorStateComponent } from '../../../../shared/table/components/error-state/error-state';
import { DeleteConfirmationComponent } from '../../../../shared/dialogs/components/delete-confirmation/delete-confirmation';

@Component({
  selector: 'app-purchase-order-list',
  standalone: true,
  imports: [
    CommonModule, 
    TableModule, 
    ButtonModule, 
    TagModule,
    TooltipModule,
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

  columns = [
    { field: 'orderNumber', header: 'Order Number' },
    { field: 'vendorName', header: 'Vendor' },
    { field: 'status', header: 'Status' },
    { field: 'orderDate', header: 'Order Date' },
    { field: 'totalAmount', header: 'Total Amount' },
    { field: 'requestedBy', header: 'Requested By' }
  ];
  
  canImport = this.authService.hasPermission(PERMISSIONS.poImports.view);
  canViewDetails = this.authService.hasPermission(PERMISSIONS.poImports.view);
  canDelete = this.authService.hasPermission(PERMISSIONS.poImports.delete);

  deleteDialogVisible = false;
  purchaseOrderToDelete: string | null = null;

  ngOnInit() {
    this.store.loadPurchaseOrders();
  }

  onImport() {
    this.router.navigate(['/dashboard/purchase-orders/import']);
  }

  onViewDetails(id: string) {
    this.router.navigate(['/dashboard/purchase-orders', id]);
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
