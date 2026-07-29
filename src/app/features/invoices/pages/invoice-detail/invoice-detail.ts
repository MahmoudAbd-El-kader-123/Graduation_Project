import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { TagModule } from 'primeng/tag';
import { CardModule } from 'primeng/card';
import { TableModule } from 'primeng/table';
import { TooltipModule } from 'primeng/tooltip';
import { InvoiceService } from '../../services/invoice.service';
import { InvoiceFacade } from '../../facades/invoice.facade';
import { InvoiceDetailDto } from '../../models/invoice.model';

@Component({
  selector: 'app-invoice-detail',
  standalone: true,
  imports: [
    CommonModule,
    ButtonModule,
    TagModule,
    CardModule,
    TableModule,
    TooltipModule
  ],
  templateUrl: './invoice-detail.html'
})
export class InvoiceDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly service = inject(InvoiceService);
  readonly facade = inject(InvoiceFacade);

  readonly invoice = signal<InvoiceDetailDto | null>(null);
  readonly loading = signal<boolean>(false);
  readonly error = signal<string | null>(null);

  ngOnInit(): void {
    const idParam = this.route.snapshot.paramMap.get('id');
    const id = idParam ? parseInt(idParam, 10) : null;
    if (!id || isNaN(id)) {
      this.error.set('Invalid invoice ID');
      return;
    }
    this._loadInvoice(id);
  }

  goBack(): void {
    this.router.navigate(['/dashboard/invoices']);
  }

  downloadInvoice(): void {
    const inv = this.invoice();
    if (inv) {
      this.facade.downloadInvoice(inv.id);
    }
  }

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

  formatAmount(amount: number): string {
    return new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(amount);
  }

  formatDate(dateStr: string | null | undefined): string {
    if (!dateStr) return '—';
    return new Date(dateStr).toLocaleDateString('en-US', {
      year: 'numeric', month: 'short', day: '2-digit'
    });
  }

  private _loadInvoice(id: number): void {
    this.loading.set(true);
    this.service.getInvoice(id).subscribe({
      next: response => {
        if (response.success && response.data) {
          this.invoice.set(response.data);
        } else {
          this.error.set(response.message ?? 'Invoice not found');
        }
        this.loading.set(false);
      },
      error: () => {
        this.error.set('An error occurred while loading the invoice');
        this.loading.set(false);
      }
    });
  }
}
