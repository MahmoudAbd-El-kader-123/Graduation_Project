import { Injectable, computed, inject, signal } from '@angular/core';
import { PurchaseOrder, PurchaseOrderStatus } from '../models/purchase-order.model';
import { PurchaseOrderFilter } from '../models/purchase-order-filter.model';
import { PurchaseOrderApiService } from './purchase-order-api.service';
import { MessageService } from 'primeng/api';
import { firstValueFrom } from 'rxjs';

const ALL_ITEMS_PAGE_SIZE = 1000;

@Injectable({
  providedIn: 'root'
})
export class PurchaseOrderStoreService {
  private api = inject(PurchaseOrderApiService);
  private messageService = inject(MessageService);

  // Raw dataset
  readonly rawPurchaseOrders = signal<PurchaseOrder[]>([]);

  // Filter State
  readonly searchTerm = signal<string>('');
  readonly selectedVendorId = signal<string | null>(null);
  readonly selectedStatus = signal<PurchaseOrderStatus | null>(null);

  // Pagination State
  readonly pageNumber = signal<number>(1);
  readonly pageSize = signal<number>(10);

  // UI State
  readonly loading = signal<boolean>(false);
  readonly error = signal<string | null>(null);
  readonly selectedPurchaseOrder = signal<PurchaseOrder | null>(null);
  readonly detailsLoading = signal<boolean>(false);
  readonly detailsError = signal<string | null>(null);
  readonly deleteLoading = signal<boolean>(false);

  // Computed: Filtered Data
  readonly filteredPurchaseOrders = computed(() => {
    const search = this.searchTerm().trim().toLowerCase();
    const vendorId = this.selectedVendorId();
    const status = this.selectedStatus();

    return this.rawPurchaseOrders().filter(order => {
      const matchesSearch =
        !search ||
        order.orderNumber?.toLowerCase().includes(search) ||
        order.vendorName?.toLowerCase().includes(search) ||
        order.requestedByUserName?.toLowerCase().includes(search);

      const matchesVendor =
        vendorId === null ||
        order.vendorId === vendorId;

      const matchesStatus =
        status === null ||
        order.status === status;

      return matchesSearch && matchesVendor && matchesStatus;
    });
  });

  // Computed: Paginated Data
  readonly paginatedPurchaseOrders = computed(() => {
    const items = this.filteredPurchaseOrders();
    const start = (this.pageNumber() - 1) * this.pageSize();
    const end = start + this.pageSize();
    return items.slice(start, end);
  });

  // Computed: Metadata
  readonly totalCount = computed(() => this.filteredPurchaseOrders().length);

  readonly state = computed(() => {
    if (this.loading()) return 'loading';
    if (this.error()) return 'error';
    if (this.filteredPurchaseOrders().length === 0) return 'empty';
    return 'data';
  });

  // Actions
  async loadPurchaseOrders() {
    this.loading.set(true);
    this.error.set(null);

    try {
      let currentPage = 1;
      let hasNextPage = true;
      const allItems: PurchaseOrder[] = [];

      while (hasNextPage) {
        const filter: PurchaseOrderFilter = {
          pageNumber: currentPage,
          pageSize: ALL_ITEMS_PAGE_SIZE
        };
        const response = await firstValueFrom(this.api.getPurchaseOrders(filter));
        
        if (response.success && response.data) {
          allItems.push(...(response.data.items || []));
          hasNextPage = response.data.hasNextPage;
          currentPage++;
        } else {
          this.error.set(response.message || 'Failed to load purchase orders');
          this.messageService.add({ severity: 'error', summary: 'Error', detail: this.error()! });
          break;
        }
      }

      if (!this.error()) {
        this.rawPurchaseOrders.set(allItems);
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
        this.loadPurchaseOrders(); // Refresh from backend
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

  // Filter Setters
  setSearchTerm(term: string) {
    this.searchTerm.set(term);
    this.pageNumber.set(1);
  }

  setVendor(vendorId: string | null) {
    this.selectedVendorId.set(vendorId);
    this.pageNumber.set(1);
  }

  setStatus(status: PurchaseOrderStatus | null) {
    this.selectedStatus.set(status);
    this.pageNumber.set(1);
  }

  setPage(pageNumber: number, pageSize: number) {
    this.pageNumber.set(pageNumber);
    this.pageSize.set(pageSize);
  }

  clearFilters() {
    this.searchTerm.set('');
    this.selectedVendorId.set(null);
    this.selectedStatus.set(null);
    this.pageNumber.set(1);
  }

  invalidateCache() {
    this.clearFilters();
    this.rawPurchaseOrders.set([]);
  }
}
