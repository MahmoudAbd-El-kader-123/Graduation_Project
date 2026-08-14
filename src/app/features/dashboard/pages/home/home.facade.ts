import { Injectable, inject, signal, computed } from '@angular/core';
import { finalize } from 'rxjs';
import { HomeService } from './home.service';
import { DashboardStats } from '../../models/dashboard-stats.model';
import { AuthStorageService } from '../../../../core/auth/services/auth-storage.service';

export interface UsersPerRoleChartData {
  labels: string[];
  values: number[];
}

@Injectable({
  providedIn: 'root'
})
export class HomeFacade {
  private readonly homeService = inject(HomeService);
  private readonly authStorage = inject(AuthStorageService);

  readonly stats = signal<DashboardStats | null>(null);
  readonly isLoading = signal<boolean>(false);
  readonly error = signal<string | null>(null);

  readonly hasStats = computed(() => !!this.stats());
  
  readonly usersPerRoleData = computed<UsersPerRoleChartData | null>(() => {
    const data = this.stats();
    if (!data || !data.usersPerRole) return null;
    
    const labels = Object.keys(data.usersPerRole);
    const values = Object.values(data.usersPerRole);
    
    if (labels.length === 0) return null;
    
    return {
      labels,
      values
    };
  });

  readonly userStatusData = computed(() => {
    const data = this.stats();
    if (!data) return null;

    return {
      labels: ['Active', 'Inactive'],
      values: [data.activeUsers, data.inactiveUsers]
    };
  });

  readonly vendorApprovalData = computed(() => {
    const data = this.stats();
    const vendors = data?.businessData?.vendors ?? [];
    if (!vendors.length) return null;

    return {
      labels: ['Approved', 'Pending approval'],
      values: [vendors.filter(vendor => vendor.isApproved).length, vendors.filter(vendor => !vendor.isApproved).length]
    };
  });

  readonly productsByVendorData = computed(() => {
    const products = this.stats()?.businessData?.products ?? [];
    return this.groupAndRank(products.map(product => product.vendorName || 'Unassigned'));
  });

  readonly purchaseOrderStatusData = computed(() => {
    const orders = this.stats()?.businessData?.purchaseOrders ?? [];
    const statusNames: Record<string, string> = {
      '1': 'Draft',
      '2': 'Pending approval',
      '3': 'Approved',
      '4': 'Rejected',
      '5': 'Fulfilled',
      '6': 'Cancelled'
    };
    return this.groupAndRank(
      orders.map(order => statusNames[String(order.status)] || this.formatLabel(String(order.status || 'Unknown'))),
      10
    );
  });

  readonly invoiceStatusData = computed(() => {
    const invoices = this.stats()?.businessData?.invoices ?? [];
    return this.groupAndRank(invoices.map(invoice => this.formatLabel(invoice.status || 'Unknown')), 10);
  });

  readonly invoiceValueByVendorData = computed(() => {
    const invoices = this.stats()?.businessData?.invoices ?? [];
    if (!invoices.length) return null;

    const totals = new Map<string, number>();
    for (const invoice of invoices) {
      const vendor = invoice.vendorName || 'Unassigned';
      totals.set(vendor, (totals.get(vendor) ?? 0) + Number(invoice.totalAmount || 0));
    }
    const ranked = [...totals.entries()].sort((a, b) => b[1] - a[1]).slice(0, 8);

    return {
      labels: ranked.map(([label]) => label),
      values: ranked.map(([, value]) => value)
    };
  });

  readonly reconciliationHealthData = computed(() => {
    const reports = this.stats()?.businessData?.reports ?? [];
    const records = reports.length ? reports : (this.stats()?.businessData?.invoices ?? []);
    if (!records.length) return null;

    const withIssues = records.filter(record => record.hasDiscrepancies).length;
    return {
      labels: ['Clean match', 'Needs review'],
      values: [records.length - withIssues, withIssues]
    };
  });

  loadStats(): void {
    this.isLoading.set(true);
    this.error.set(null);

    this.homeService.getStats(this.authStorage.permissions()).pipe(
      finalize(() => this.isLoading.set(false))
    ).subscribe({
      next: (stats) => {
        this.stats.set(stats);
      },
      error: (err) => {
        this.error.set(err?.error?.message || err?.message || 'An error occurred while loading dashboard statistics.');
      }
    });
  }

  private groupAndRank(values: string[], limit = 8): UsersPerRoleChartData | null {
    if (!values.length) return null;

    const counts = new Map<string, number>();
    for (const value of values) counts.set(value, (counts.get(value) ?? 0) + 1);
    const ranked = [...counts.entries()].sort((a, b) => b[1] - a[1]).slice(0, limit);

    return {
      labels: ranked.map(([label]) => label),
      values: ranked.map(([, value]) => value)
    };
  }

  private formatLabel(value: string): string {
    return value
      .replace(/([a-z])([A-Z])/g, '$1 $2')
      .replace(/[_-]+/g, ' ')
      .replace(/\b\w/g, character => character.toUpperCase());
  }
}
