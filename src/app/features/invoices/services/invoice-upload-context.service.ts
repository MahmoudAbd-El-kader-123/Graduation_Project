import { Injectable, signal } from '@angular/core';
import { PurchaseOrder } from '../../purchase-orders/models/purchase-order.model';

@Injectable({
  providedIn: 'root'
})
export class InvoiceUploadContextService {
  private readonly contextPo = signal<PurchaseOrder | null>(null);

  setPurchaseOrder(po: PurchaseOrder): void {
    this.contextPo.set(po);
  }

  consumePurchaseOrder(): PurchaseOrder | null {
    const po = this.contextPo();
    this.clear();
    return po;
  }

  clear(): void {
    this.contextPo.set(null);
  }
}
