import { Injectable } from '@angular/core';
import { ProductService } from '../api/services/product.service';
import { ProductStore } from '../stores/product.store';
import { ProductRequest } from '../models/product.model';
import { MessageService } from 'primeng/api';

@Injectable()
export class ProductFacade {
  readonly state;
  readonly items;
  readonly totalCount;
  readonly pageNumber;
  readonly pageSize;
  readonly searchTerm;
  readonly loading;

  readonly createLoading;
  readonly updateLoading;
  readonly deleteLoading;
  readonly selectedItem;

  constructor(
    private readonly service: ProductService,
    private readonly store: ProductStore,
    private readonly messageService: MessageService
  ) {
    this.state = this.store.table.state;
    this.items = this.store.table.items;
    this.totalCount = this.store.table.totalCount;
    this.pageNumber = this.store.table.pageNumber;
    this.pageSize = this.store.table.pageSize;
    this.searchTerm = this.store.table.searchTerm;
    this.loading = this.store.table.loading;

    this.createLoading = this.store.crud.createLoading;
    this.updateLoading = this.store.crud.updateLoading;
    this.deleteLoading = this.store.crud.deleteLoading;
    this.selectedItem = this.store.crud.selectedItem;
  }

  loadProducts() {
    this.store.table.setLoading(true);
    this.service.getProducts(this.pageNumber(), this.pageSize(), this.searchTerm())
      .subscribe({
        next: (response) => {
          if (response.success && response.data) {
            this.store.table.setItems(response.data.items, response.data.totalCount);
          } else {
            this.store.table.setError(response.message || 'Failed to load products');
          }
          this.store.table.setLoading(false);
        },
        error: () => {
          this.store.table.setError('An error occurred while loading products');
          this.store.table.setLoading(false);
        }
      });
  }

  setPage(pageNumber: number, pageSize: number) {
    this.store.table.setPage(pageNumber, pageSize);
    this.loadProducts();
  }

  setSearchTerm(term: string) {
    this.store.table.setSearchTerm(term);
    this.loadProducts();
  }

  loadProduct(id: number | string) {
    this.store.crud.setUpdateLoading(true);
    this.service.getProduct(id).subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.store.crud.selectItem(response.data, id);
        } else {
          this.messageService.add({ severity: 'error', summary: 'Error', detail: response.message || 'Failed to load product' });
        }
        this.store.crud.setUpdateLoading(false);
      },
      error: () => {
        this.messageService.add({ severity: 'error', summary: 'Error', detail: 'An error occurred while loading product' });
        this.store.crud.setUpdateLoading(false);
      }
    });
  }

  createProduct(product: ProductRequest, router: any) {
    this.store.crud.setCreateLoading(true);
    this.service.createProduct(product).subscribe({
      next: (response) => {
        if (response.success) {
          this.messageService.add({ severity: 'success', summary: 'Success', detail: 'Product created successfully' });
          router.navigate(['/dashboard/products']);
        } else {
          this.messageService.add({ severity: 'error', summary: 'Error', detail: response.message || 'Failed to create product' });
        }
        this.store.crud.setCreateLoading(false);
      },
      error: (err) => {
        let detail = 'An error occurred';
        if (err.status === 400) {
          detail = err.error?.message || 'Validation error';
        } else if (err.status === 404) {
          detail = 'Product not found.';
        }
        this.messageService.add({ severity: 'error', summary: 'Error', detail });
        this.store.crud.setCreateLoading(false);
      }
    });
  }

  updateProduct(id: number | string, product: ProductRequest, router: any) {
    this.store.crud.setUpdateLoading(true);
    this.service.updateProduct(id, product).subscribe({
      next: (response) => {
        if (response.success) {
          this.messageService.add({ severity: 'success', summary: 'Success', detail: 'Product updated successfully' });
          router.navigate(['/dashboard/products']);
        } else {
          this.messageService.add({ severity: 'error', summary: 'Error', detail: response.message || 'Failed to update product' });
        }
        this.store.crud.setUpdateLoading(false);
      },
      error: (err) => {
        let detail = 'An error occurred';
        if (err.status === 400) {
          detail = err.error?.message || 'Validation error';
        } else if (err.status === 404) {
          detail = 'Product not found.';
        }
        this.messageService.add({ severity: 'error', summary: 'Error', detail });
        this.store.crud.setUpdateLoading(false);
      }
    });
  }

  deleteProduct(id: number | string, onSuccess?: () => void) {
    this.store.crud.setDeleteLoading(true);
    this.service.deleteProduct(id).subscribe({
      next: (response) => {
        if (response.success) {
          this.messageService.add({ severity: 'success', summary: 'Success', detail: 'Product deleted successfully' });
          this.loadProducts();
          if (onSuccess) onSuccess();
        } else {
          this.messageService.add({ severity: 'error', summary: 'Error', detail: response.message || 'Failed to delete product' });
        }
        this.store.crud.setDeleteLoading(false);
      },
      error: () => {
        this.messageService.add({ severity: 'error', summary: 'Error', detail: 'An error occurred' });
        this.store.crud.setDeleteLoading(false);
      }
    });
  }
}
