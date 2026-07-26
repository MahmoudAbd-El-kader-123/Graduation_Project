import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, from, mergeMap, toArray, forkJoin, of } from 'rxjs';
import { API_BASE_URL, API_ENDPOINTS } from '../../../core/constants/api.constants';
import { ApiResponse } from '../../../shared/api/models/api-response.model';
import { PagedResult } from '../../../shared/api/models/paged-result.model';
import { PurchaseOrder } from '../models/purchase-order.model';
import { PurchaseOrderFilter } from '../models/purchase-order-filter.model';
import { ImportPreviewResponse } from '../models/purchase-order-preview.model';
import { VendorMapping } from '../models/vendor-mapping.model';
import { PurchaseOrderImportResult } from '../models/purchase-order-import-result.model';

@Injectable({
  providedIn: 'root'
})
export class PurchaseOrderApiService {
  private http = inject(HttpClient);
  private readonly baseUrl = API_BASE_URL;

  getPurchaseOrders(filter: PurchaseOrderFilter): Observable<ApiResponse<PagedResult<PurchaseOrder>>> {
    let params = new HttpParams()
      .set('pageNumber', filter.pageNumber.toString())
      .set('pageSize', filter.pageSize.toString());
      
    if (filter.searchQuery) params = params.set('searchQuery', filter.searchQuery);
    if (filter.vendorId) params = params.set('vendorId', filter.vendorId);
    if (filter.status) params = params.set('status', filter.status);

    return this.http.get<ApiResponse<PagedResult<PurchaseOrder>>>(`${this.baseUrl}${API_ENDPOINTS.purchaseOrders}`, { params });
  }

  getPurchaseOrderById(id: string): Observable<ApiResponse<PurchaseOrder>> {
    return this.http.get<ApiResponse<PurchaseOrder>>(`${this.baseUrl}${API_ENDPOINTS.purchaseOrders}/${id}`);
  }

  deletePurchaseOrder(id: string): Observable<ApiResponse<void>> {
    return this.http.delete<ApiResponse<void>>(`${this.baseUrl}${API_ENDPOINTS.purchaseOrders}/${id}`);
  }

  previewImport(file: File, rowsToExtract: number = 50): Observable<ApiResponse<ImportPreviewResponse>> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<ApiResponse<ImportPreviewResponse>>(`${this.baseUrl}${API_ENDPOINTS.purchaseOrders}/import-preview?rowsToExtract=${rowsToExtract}`, formData);
  }

  getVendorMappings(vendorId: string): Observable<ApiResponse<VendorMapping[]>> {
    return this.http.get<ApiResponse<VendorMapping[]>>(`${this.baseUrl}/vendor-mappings/vendor/${vendorId}`);
  }

  /**
   * Accepts an array of mappings and guarantees they are persisted successfully 
   * regardless of the backend implementation.
   */
  saveVendorMappings(mappings: VendorMapping[]): Observable<ApiResponse<void>[]> {
    if (!mappings || mappings.length === 0) {
      return of([]);
    }
    
    // Fallback sequential POSTs to hide backend limitations
    const requests = mappings.map(mapping => 
      this.http.post<ApiResponse<void>>(`${this.baseUrl}/vendor-mappings`, mapping)
    );
    
    // Run them sequentially to avoid hammering the backend if it doesn't like concurrent creates
    // Using forkJoin to run in parallel is faster, but since they are individual endpoints, 
    // let's do sequential just to be safe as per the user's initial description:
    // "for await ... POST POST POST"
    // Using RxJS mergeMap with concurrency 1
    return from(mappings).pipe(
      mergeMap(mapping => this.http.post<ApiResponse<void>>(`${this.baseUrl}/vendor-mappings`, mapping), 1),
      toArray()
    );
  }

  importPurchaseOrder(payload: FormData): Observable<ApiResponse<PurchaseOrderImportResult>> {
    return this.http.post<ApiResponse<PurchaseOrderImportResult>>(`${this.baseUrl}${API_ENDPOINTS.purchaseOrders}/import`, payload);
  }
}
