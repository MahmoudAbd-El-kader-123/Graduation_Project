import { Component, OnInit, inject, DestroyRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { PurchaseOrderStoreService } from '../../services/purchase-order-store.service';
import { PurchaseOrder, PurchaseOrderStatus } from '../../models/purchase-order.model';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { TooltipModule } from 'primeng/tooltip';
import { SkeletonModule } from 'primeng/skeleton';
import { LucideArrowLeft as ArrowLeft, LucideDownload as Download, LucidePrinter as Printer, LucideFileDown as FileDown } from '@lucide/angular';

import { EmptyStateComponent } from '../../../../shared/table/components/empty-state/empty-state';
import { ErrorStateComponent } from '../../../../shared/table/components/error-state/error-state';
import { AuthService } from '../../../../core/auth/services/auth.service';
import { PERMISSIONS } from '../../../../core/auth/constants/permissions';
import { InvoiceUploadContextService } from '../../../invoices/services/invoice-upload-context.service';

@Component({
  selector: 'app-purchase-order-details',
  standalone: true,
  imports: [
    CommonModule,
    ButtonModule,
    CardModule,
    TableModule,
    TagModule,
    TooltipModule,
    SkeletonModule,
    EmptyStateComponent,
    ErrorStateComponent
  ],
  templateUrl: './purchase-order-details.component.html'
})
export class PurchaseOrderDetailsComponent implements OnInit {
  store = inject(PurchaseOrderStoreService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private destroyRef = inject(DestroyRef);
  private authService = inject(AuthService);
  private uploadContextService = inject(InvoiceUploadContextService);

  readonly icons = { ArrowLeft, Download, Printer, FileDown };

  canUploadInvoice = this.authService.hasPermission(PERMISSIONS.invoices.upload);

  ngOnInit() {
    this.route.paramMap
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(params => {
        const id = params.get('id');
        if (id) {
          this.store.loadPurchaseOrder(id);
        }
      });
  }

  goBack() {
    this.router.navigate(['/dashboard/purchase-orders']);
  }

  getStatusLabel(status: PurchaseOrderStatus | undefined): string {
    switch (status) {
      case PurchaseOrderStatus.Draft: return 'Draft';
      case PurchaseOrderStatus.PendingApproval: return 'Pending Approval';
      case PurchaseOrderStatus.Approved: return 'Approved';
      case PurchaseOrderStatus.Rejected: return 'Rejected';
      case PurchaseOrderStatus.Fulfilled: return 'Completed'; // User explicitly said Completed in the example
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
      case PurchaseOrderStatus.Cancelled: return 'danger';
      default: return 'secondary';
    }
  }

  onUploadInvoice() {
    const po = this.store.selectedPurchaseOrder();
    if (po) {
      this.uploadContextService.setPurchaseOrder(po);
      this.router.navigate(['/dashboard/invoices']);
    }
  }
}
