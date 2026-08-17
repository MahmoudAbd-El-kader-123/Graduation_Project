import { LucideDynamicIcon } from '@lucide/angular';
import { Component, OnInit, computed, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HomeFacade } from './home.facade';
import { AuthStorageService } from '../../../../core/auth/services/auth-storage.service';
import { SkeletonModule } from 'primeng/skeleton';
import { BaseChartDirective, provideCharts, withDefaultRegisterables } from 'ng2-charts';
import { ChartConfiguration, ChartData } from 'chart.js';
import { ThemeService } from '../../../../core/services/theme.service';
import { DashboardStats } from '../../models/dashboard-stats.model';
import { AppIcon } from '../../../../core/icons/lucide-icons';
import { LanguageService } from '../../../../core/i18n/language.service';

@Component({
  selector: 'app-dashboard-home',
  standalone: true,
  imports: [CommonModule, SkeletonModule, BaseChartDirective, LucideDynamicIcon],
  providers: [provideCharts(withDefaultRegisterables())],
  templateUrl: './home.component.html'
})
export class HomeComponent implements OnInit {
  public readonly facade = inject(HomeFacade);
  public readonly authStorage = inject(AuthStorageService);
  private readonly themeService = inject(ThemeService);
  private readonly languageService = inject(LanguageService);

  readonly kpis: Array<{ label: string; field: keyof DashboardStats; icon: AppIcon }> = [
    { label: 'Vendors', field: 'totalVendors', icon: 'building-2' },
    { label: 'Products', field: 'totalProducts', icon: 'package' },
    { label: 'Purchase Orders', field: 'totalPurchaseOrders', icon: 'file-text' },
    { label: 'Invoices', field: 'totalInvoices', icon: 'receipt' },
    { label: 'Users', field: 'totalUsers', icon: 'users' },
    { label: 'Admins', field: 'totalAdmins', icon: 'shield' }
  ];

  readonly vendorApprovalChartData = computed<ChartData<'doughnut'>>(() => {
    const data = this.facade.vendorApprovalData();
    return {
      labels: this.translateLabels(data?.labels),
      datasets: [{
        data: data?.values ?? [],
        backgroundColor: ['#F5A623', '#243B55'],
        borderWidth: 0,
        hoverOffset: 8
      }]
    };
  });

  readonly reconciliationChartData = computed<ChartData<'doughnut'>>(() => {
    const data = this.facade.reconciliationHealthData();
    return {
      labels: this.translateLabels(data?.labels),
      datasets: [{
        data: data?.values ?? [],
        backgroundColor: ['#F5A623', '#E8940D'],
        borderWidth: 0,
        hoverOffset: 8
      }]
    };
  });

  readonly productsByVendorChartData = computed<ChartData<'bar'>>(() => {
    const data = this.facade.productsByVendorData();
    return {
      labels: this.translateLabels(data?.labels),
      datasets: [{
        label: this.languageService.translate('Products'),
        data: data?.values ?? [],
        backgroundColor: '#1B2E45',
        borderRadius: 8,
        maxBarThickness: 48
      }]
    };
  });

  readonly purchaseOrderStatusChartData = computed<ChartData<'bar'>>(() => {
    const data = this.facade.purchaseOrderStatusData();
    return this.countBarData(data, 'Purchase orders', '#F5A623');
  });

  readonly invoiceStatusChartData = computed<ChartData<'bar'>>(() => {
    const data = this.facade.invoiceStatusData();
    return this.countBarData(data, 'Invoices', '#E8940D');
  });

  readonly invoiceValueChartData = computed<ChartData<'bar'>>(() => {
    const data = this.facade.invoiceValueByVendorData();
    return this.countBarData(data, 'Invoice value', '#f59e0b');
  });

  readonly doughnutOptions = computed<ChartConfiguration<'doughnut'>['options']>(() => ({
    responsive: true,
    maintainAspectRatio: false,
    cutout: '65%',
    plugins: {
      legend: {
        position: 'bottom',
        labels: {
          color: this.chartTextColor(),
          usePointStyle: true,
          padding: 18
        }
      },
      tooltip: { enabled: true }
    }
  }));

  readonly countBarOptions = computed<ChartConfiguration<'bar'>['options']>(() => ({
    responsive: true,
    maintainAspectRatio: false,
    indexAxis: 'y',
    plugins: {
      legend: { display: false }
    },
    scales: {
      x: {
        beginAtZero: true,
        ticks: { color: this.chartTextColor(), precision: 0 },
        grid: { color: this.chartGridColor() }
      },
      y: {
        grid: { display: false },
        ticks: { color: this.chartTextColor() }
      }
    }
  }));

  readonly currencyBarOptions = computed<ChartConfiguration<'bar'>['options']>(() => ({
    ...this.countBarOptions(),
    plugins: {
      legend: { display: false },
      tooltip: {
        callbacks: {
          label: context => new Intl.NumberFormat(this.languageService.isArabic() ? 'ar-EG' : 'en-EG', {
            style: 'currency',
            currency: 'EGP',
            maximumFractionDigits: 0
          }).format(Number(context.parsed.x ?? 0))
        }
      }
    }
  }));

  ngOnInit(): void {
    this.facade.loadStats();
  }

  getWidgetValue(field: keyof DashboardStats): number | null {
    const stats = this.facade.stats();
    if (!stats) return null;
    const value = stats[field];
    return typeof value === 'number' ? value : null;
  }

  private countBarData(
    data: { labels: string[]; values: number[] } | null,
    label: string,
    color: string
  ): ChartData<'bar'> {
    return {
      labels: this.translateLabels(data?.labels),
      datasets: [{
        label: this.languageService.translate(label),
        data: data?.values ?? [],
        backgroundColor: color,
        borderRadius: 8,
        maxBarThickness: 48
      }]
    };
  }

  private isDarkTheme(): boolean {
    const theme = this.themeService.currentTheme();
    return theme === 'dark' || (theme === 'system' && window.matchMedia('(prefers-color-scheme: dark)').matches);
  }

  private translateLabels(labels: string[] | undefined): string[] {
    return (labels ?? []).map(label => this.languageService.translate(label));
  }

  private chartTextColor(): string {
    return this.isDarkTheme() ? '#94A3B8' : '#334155';
  }

  private chartGridColor(): string {
    return this.isDarkTheme() ? 'rgba(36, 59, 85, 0.6)' : 'rgba(203, 213, 225, 0.5)';
  }
}
