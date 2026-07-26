import { FormGroup } from '@angular/forms';
import { ProductRequest } from '../dtos/product-request.dto';
import { Product } from '../dtos/product-response.dto';
import { ProductFormControls } from '../../configs/product-form.config';

export class ProductMapper {
  static mapResponseToForm(product: Product, form: FormGroup<ProductFormControls>): void {
    form.patchValue({
      erpId: product.erpId,
      name: product.name,
      skuSupplier: product.skuSupplier || '',
      skuRetailer: product.skuRetailer,
      barcode: product.barcode,
      description: product.description,
      unitPrice: product.unitPrice,
      uom: product.uom || '',
      vendorId: product.vendorId
    });
  }

  static mapFormToRequest(form: FormGroup<ProductFormControls>): ProductRequest {
    const raw = form.getRawValue();
    return {
      erpId: raw.erpId,
      name: raw.name,
      skuSupplier: raw.skuSupplier || null,
      skuRetailer: raw.skuRetailer || null,
      barcode: raw.barcode || null,
      description: raw.description || null,
      unitPrice: Number(raw.unitPrice),
      uom: raw.uom,
      vendorId: Number(raw.vendorId)
    };
  }
}
