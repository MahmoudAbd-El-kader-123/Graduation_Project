import { Component, computed, input } from '@angular/core';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import {
  DISCREPANCY_LABELS,
  getReconciliationViewState
} from '../../constants/invoice-reconciliation.constants';
import { DiscrepancyType, InvoiceReconciliation } from '../../models/invoice.model';

@Component({
  selector: 'app-invoice-reconciliation',
  imports: [ProgressSpinnerModule, TableModule, TagModule],
  templateUrl: './invoice-reconciliation.component.html'
})
export class InvoiceReconciliationComponent {
  readonly reconciliation = input<InvoiceReconciliation | null>(null);
  readonly loading = input(true);
  readonly error = input<string | null>(null);

  readonly viewState = computed(() => getReconciliationViewState(this.reconciliation()));

  getDiscrepancyLabel(discrepancyType: DiscrepancyType): string {
    return DISCREPANCY_LABELS[discrepancyType];
  }

  getFieldLabel(fieldName: string): string {
    switch (fieldName) {
      case 'TotalAmount': return 'Invoice total';
      case 'UnitPrice': return 'Unit price';
      case 'Amount': return 'Line amount';
      default: return fieldName;
    }
  }
}
