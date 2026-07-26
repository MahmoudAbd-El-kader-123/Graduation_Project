export interface PurchaseOrderFilter {
  searchQuery?: string;
  vendorId?: string;
  status?: string;
  pageNumber: number;
  pageSize: number;
}
