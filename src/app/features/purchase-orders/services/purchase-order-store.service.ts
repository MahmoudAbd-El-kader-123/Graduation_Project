import { Injectable, computed, inject, signal } from '@angular/core';
import { PurchaseOrder } from '../models/purchase-order.model';
import { PurchaseOrderFilter } from '../models/purchase-order-filter.model';
import { PurchaseOrderApiService } from './purchase-order-api.service';
import { MessageService } from 'primeng/api';
import { firstValueFrom } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class PurchaseOrderStoreService {
  private api = inject(PurchaseOrderApiService);
  private messageService = inject(MessageService);

  // State
  readonly loading = signal<boolean>(false);
  readonly error = signal<string | null>(null);
  readonly purchaseOrders = signal<PurchaseOrder[]>([]);
  readonly totalCount = signal<number>(0);
  readonly selectedPurchaseOrder = signal<PurchaseOrder | null>(null);
  readonly detailsLoading = signal<boolean>(false);
  readonly detailsError = signal<string | null>(null);
  readonly deleteLoading = signal<boolean>(false);
  
  readonly filters = signal<PurchaseOrderFilter>({
    pageNumber: 1,
    pageSize: 10,
  });

  // Computed
  readonly state = computed(() => {
    if (this.loading()) return 'loading';
    if (this.error()) return 'error';
    if (this.purchaseOrders().length === 0) return 'empty';
    return 'data';
  });

  readonly searchTerm = computed(() => this.filters().searchQuery || '');
  readonly pageNumber = computed(() => this.filters().pageNumber);
  readonly pageSize = computed(() => this.filters().pageSize);

  // Actions
  async loadPurchaseOrders() {
    this.loading.set(true);
    this.error.set(null);

    try {
      const response = await firstValueFrom(this.api.getPurchaseOrders(this.filters()));
      if (response.success && response.data) {
        this.purchaseOrders.set(response.data.items || []);
        this.totalCount.set(response.data.totalCount || 0);
      } else {
        this.error.set(response.message || 'Failed to load purchase orders');
        this.messageService.add({ severity: 'error', summary: 'Error', detail: this.error()! });
      }
    } catch (err) {
      this.error.set('An unexpected error occurred while loading purchase orders.');
      this.messageService.add({ severity: 'error', summary: 'Error', detail: this.error()! });
    } finally {
      this.loading.set(false);
    }
  }

  async loadPurchaseOrder(id: string) {
    this.detailsLoading.set(true);
    this.detailsError.set(null);
    this.selectedPurchaseOrder.set(null);

    try {
      const response = await firstValueFrom(this.api.getPurchaseOrderById(id));
      if (response.success && response.data) {
        this.selectedPurchaseOrder.set(response.data);
      } else {
        this.detailsError.set(response.message || 'Failed to load purchase order details');
      }
    } catch (err) {
      this.detailsError.set('An unexpected error occurred while loading purchase order details.');
    } finally {
      this.detailsLoading.set(false);
    }
  }

  async deletePurchaseOrder(id: string, onSuccess?: () => void) {
    this.deleteLoading.set(true);
    try {
      const response = await firstValueFrom(this.api.deletePurchaseOrder(id));
      if (response.success) {
        this.messageService.add({ severity: 'success', summary: 'Success', detail: 'Purchase order deleted successfully' });
        this.loadPurchaseOrders();
        if (onSuccess) onSuccess();
      } else {
        this.messageService.add({ severity: 'error', summary: 'Error', detail: response.message || 'Failed to delete purchase order' });
      }
    } catch (err) {
      this.messageService.add({ severity: 'error', summary: 'Error', detail: 'An unexpected error occurred while deleting the purchase order' });
    } finally {
      this.deleteLoading.set(false);
    }
  }

  setSearchTerm(term: string) {
    this.filters.update(f => ({ ...f, searchQuery: term, pageNumber: 1 }));
    this.loadPurchaseOrders();
  }

  setVendor(vendorId: string | null) {
    this.filters.update(f => {
      const updated = { ...f, pageNumber: 1 };
      if (vendorId) updated.vendorId = vendorId;
      else delete updated.vendorId;
      return updated;
    });
    this.loadPurchaseOrders();
  }

  setStatus(status: string | null) {
    this.filters.update(f => {
      const updated = { ...f, pageNumber: 1 };
      if (status) updated.status = status;
      else delete updated.status;
      return updated;
    });
    this.loadPurchaseOrders();
  }

  setPage(pageNumber: number, pageSize: number) {
    this.filters.update(f => ({ ...f, pageNumber, pageSize }));
    this.loadPurchaseOrders();
  }

  invalidateCache() {
    // Reset to page 1 and clear search to ensure fresh data view on next load
    this.filters.update(f => ({ ...f, pageNumber: 1, searchQuery: undefined }));
    this.purchaseOrders.set([]);
  }
}
