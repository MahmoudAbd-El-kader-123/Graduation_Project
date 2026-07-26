import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../../../shared/api/services/api.service';
import { ApiResponse } from '../../../../shared/api/models/api-response.model';
import { PagedResult } from '../../../../shared/api/models/paged-result.model';
import { Product } from '../dtos/product-response.dto';
import { ProductRequest } from '../../models/product.model';
@Injectable({
  providedIn: 'root'
})
export class ProductService {
  private readonly apiService = inject(ApiService);
  private readonly baseUrl = '/products';

  getProducts(pageNumber: number, pageSize: number, searchTerm: string = ''): Observable<ApiResponse<PagedResult<Product>>> {
    let url = `${this.baseUrl}?pageNumber=${pageNumber}&pageSize=${pageSize}`;
    if (searchTerm) {
      url += `&SearchTerm=${encodeURIComponent(searchTerm)}`;
    }
    return this.apiService.get<ApiResponse<PagedResult<Product>>>(url);
  }

  getProduct(id: number | string): Observable<ApiResponse<Product>> {
    return this.apiService.get<ApiResponse<Product>>(`${this.baseUrl}/${id}`);
  }

  createProduct(product: ProductRequest): Observable<ApiResponse<Product>> {
    return this.apiService.post<ApiResponse<Product>>(this.baseUrl, product);
  }

  updateProduct(id: number | string, product: ProductRequest): Observable<ApiResponse<Product>> {
    return this.apiService.put<ApiResponse<Product>>(`${this.baseUrl}/${id}`, product);
  }

  deleteProduct(id: number | string): Observable<ApiResponse<void>> {
    return this.apiService.delete<ApiResponse<void>>(`${this.baseUrl}/${id}`);
  }
}
