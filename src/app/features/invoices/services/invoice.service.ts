import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ApiService } from '../../../shared/api/services/api.service';
import { ApiResponse } from '../../../shared/api/models/api-response.model';
import { PagedResult } from '../../../shared/api/models/paged-result.model';
import { API_ENDPOINTS, API_BASE_URL } from '../../../core/constants/api.constants';
import { InvoiceListItemDto, InvoiceDetailDto, InvoiceUploadResultDto } from '../models/invoice.model';

@Injectable()
export class InvoiceService {
  private readonly apiService = inject(ApiService);

  /**
   * ApiService intentionally remains unchanged.
   * InvoiceService injects HttpClient only for binary downloads
   * because responseType: 'blob' is outside the abstraction provided by ApiService.
   */
  private readonly http = inject(HttpClient);

  private readonly baseUrl = API_ENDPOINTS.invoices;

  /**
   * GET /api/invoices
   * Supports Status, PageNumber, PageSize filters.
   * Backend does NOT support SearchTerm — search infrastructure
   * is retained in the store but the UI input is hidden until backend adds it.
   */
  getInvoices(
    pageNumber: number,
    pageSize: number,
    status?: string
  ): Observable<ApiResponse<PagedResult<InvoiceListItemDto>>> {
    let url = `${this.baseUrl}?PageNumber=${pageNumber}&PageSize=${pageSize}`;
    if (status && status.trim().length > 0) {
      url += `&Status=${encodeURIComponent(status.trim())}`;
    }
    return this.apiService.get<ApiResponse<PagedResult<InvoiceListItemDto>>>(url);
  }

  /**
   * GET /api/invoices/{id}
   * Returns InvoiceDetailDto with full items, discrepancies, and processing logs.
   */
  getInvoice(id: number): Observable<ApiResponse<InvoiceDetailDto>> {
    return this.apiService.get<ApiResponse<InvoiceDetailDto>>(`${this.baseUrl}/${id}`);
  }

  /**
   * POST /api/invoices/upload
   * Sends File + PurchaseOrderId as multipart/form-data.
   */
  uploadInvoice(
    purchaseOrderId: string,
    file: File
  ): Observable<ApiResponse<InvoiceUploadResultDto>> {
    const formData = new FormData();
    formData.append('File', file, file.name);
    formData.append('PurchaseOrderId', purchaseOrderId);
    return this.apiService.post<ApiResponse<InvoiceUploadResultDto>>(
      `${this.baseUrl}/upload`,
      formData
    );
  }

  /**
   * GET /api/invoices/{id}/download
   * Returns a binary blob. HttpClient is used directly here because
   * ApiService does not expose responseType: 'blob'.
   * Caller should read Content-Disposition for filename; fallback: invoice-{id}.pdf
   */
  downloadInvoice(id: number): Observable<Blob> {
    return this.http.get(`${API_BASE_URL}${this.baseUrl}/${id}/download`, {
      responseType: 'blob'
    });
  }

  /**
   * Same download, but returning the full HttpResponse so the caller
   * can inspect Content-Disposition for the filename.
   */
  downloadInvoiceWithHeaders(id: number): Observable<import('@angular/common/http').HttpResponse<Blob>> {
    return this.http.get(`${API_BASE_URL}${this.baseUrl}/${id}/download`, {
      responseType: 'blob',
      observe: 'response'
    });
  }
}
