import { Component, effect, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { ProductFacade } from '../../facades/product.facade';
import { VendorLookupService } from '../../../../shared/lookups/services/vendor-lookup.service';
import { PRODUCT_FORM_CONFIG, ProductFormControls } from '../../configs/product-form.config';
import { ProductMapper } from '../../api/mappers/product.mapper';
import { CanComponentDeactivate } from '../../../../core/guards/can-deactivate.interface';

import { InputTextModule } from 'primeng/inputtext';
import { InputNumberModule } from 'primeng/inputnumber';
import { SelectModule } from 'primeng/select';
import { ButtonModule } from 'primeng/button';
import { SkeletonModule } from 'primeng/skeleton';

@Component({
  selector: 'app-product-edit',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    InputTextModule,
    InputNumberModule,
    SelectModule,
    ButtonModule,
    SkeletonModule
  ],
  templateUrl: './product-edit.html'
})
export class ProductEditComponent implements OnInit, CanComponentDeactivate {
  private readonly fb = inject(FormBuilder);
  readonly facade = inject(ProductFacade);
  readonly vendorLookup = inject(VendorLookupService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  private productId: string | null = null;

  form = this.fb.group(PRODUCT_FORM_CONFIG) as unknown as FormGroup<ProductFormControls>;

  constructor() {
    effect(() => {
      const product = this.facade.selectedItem();
      if (product && product.id.toString() === this.productId) {
        ProductMapper.mapResponseToForm(product, this.form);
        this.form.markAsPristine();
      }
    });
  }

  ngOnInit() {
    this.vendorLookup.loadVendors().subscribe();
    this.route.paramMap.subscribe(params => {
      this.productId = params.get('id');
      if (this.productId) {
        this.facade.loadProduct(this.productId);
      }
    });
  }

  canDeactivate(): boolean {
    return !this.form.dirty;
  }

  onSubmit() {
    if (this.form.valid && this.productId) {
      const payload = ProductMapper.mapFormToRequest(this.form);
      this.form.markAsPristine(); // Bypass dirty check on save
      this.facade.updateProduct(this.productId, payload, this.router);
    } else {
      this.form.markAllAsTouched();
    }
  }

  onCancel() {
    this.router.navigate(['/dashboard/products']);
  }
}
