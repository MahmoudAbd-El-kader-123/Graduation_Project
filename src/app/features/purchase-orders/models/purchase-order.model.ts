export enum PurchaseOrderStatus {
  Draft = 1,
  PendingApproval = 2,
  Approved = 3,
  Rejected = 4,
  Fulfilled = 5,
  Cancelled = 6
}

export interface PurchaseOrderItem {
  id: number;
  productId: number;
  productName: string;
  quantity: number;
  unitPrice: number;
  lineTotal: number;
}

export interface PurchaseOrder {
  id: string;
  orderNumber: string;
  vendorId: string;
  vendorName: string;
  status: PurchaseOrderStatus;
  orderDate: string;
  expectedDeliveryDate?: string | null;
  totalAmount: number;
  requestedBy: string; // Keep for backwards compat if used in list
  requestedByUserName?: string;
  requestedByUserId?: number;
  items?: PurchaseOrderItem[];
}
