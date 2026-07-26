export interface ProductRequest {
    erpId: string;
    name: string;
    skuSupplier: string | null;
    skuRetailer: string | null;
    barcode: string | null;
    description: string | null;
    unitPrice: number;
    uom: string;
    vendorId: number;
}
