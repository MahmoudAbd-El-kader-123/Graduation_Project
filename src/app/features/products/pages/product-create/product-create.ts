import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ProductFacade } from '../../facades/product.facade';
import { VendorLookupService } from '../../../../shared/lookups/services/vendor-lookup.service';
import { PRODUCT_FORM_CONFIG, ProductFormControls } from '../../configs/product-form.config';
import { ProductMapper } from '../../api/mappers/product.mapper';
import { CanComponentDeactivate } from '../../../../core/guards/can-deactivate.interface';

import { InputTextModule } from 'primeng/inputtext';
import { InputNumberModule } from 'primeng/inputnumber';
import { SelectModule } from 'primeng/select';
import { ButtonModule } from 'primeng/button';

@Component({
  selector: 'app-product-create',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    InputTextModule,
    InputNumberModule,
    SelectModule,
    ButtonModule
  ],
  templateUrl: './product-create.html'
})
export class ProductCreateComponent implements OnInit, CanComponentDeactivate {
  private readonly fb = inject(FormBuilder);
  readonly facade = inject(ProductFacade);
  readonly vendorLookup = inject(VendorLookupService);
  private readonly router = inject(Router);

  form = this.fb.group(PRODUCT_FORM_CONFIG) as unknown as FormGroup<ProductFormControls>;

  ngOnInit() {
    this.vendorLookup.loadVendors().subscribe();
  }

  canDeactivate(): boolean {
    return !this.form.dirty;
  }

  onSubmit() {
    if (this.form.valid) {
      const payload = ProductMapper.mapFormToRequest(this.form);
      this.form.markAsPristine(); // Bypass dirty check on save
      this.facade.createProduct(payload, this.router);
    } else {
      this.form.markAllAsTouched();
    }
  }

  onCancel() {
    this.router.navigate(['/dashboard/products']);
  }
}
