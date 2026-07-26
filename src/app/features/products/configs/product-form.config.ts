import { FormControl, FormGroup, Validators } from '@angular/forms';

export interface ProductFormControls {
  erpId: FormControl<string>;
  name: FormControl<string>;
  skuSupplier: FormControl<string>;
  skuRetailer: FormControl<string | null>;
  barcode: FormControl<string | null>;
  description: FormControl<string | null>;
  unitPrice: FormControl<number>;
  uom: FormControl<string>;
  vendorId: FormControl<number | null>;
}

export const PRODUCT_FORM_CONFIG = {
  erpId: ['', [Validators.required]],
  name: ['', [Validators.required]],
  skuSupplier: ['', [Validators.required]],
  skuRetailer: [null as string | null],
  barcode: [null as string | null],
  description: [null as string | null],
  unitPrice: [0, [Validators.required, Validators.min(0)]],
  uom: ['', [Validators.required]],
  vendorId: [null as number | null, [Validators.required]]
};
