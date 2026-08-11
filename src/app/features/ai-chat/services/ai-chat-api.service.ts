import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpContext } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { ApiResponse } from '../../../core/models/api-response.model';
import { SKIP_ERROR_TOAST } from '../../../core/interceptors/error.interceptor';
import {
  ChatSessionListDto,
  ChatSessionDetailDto,
  CreateChatSessionDto,
  SendChatMessageDto,
  ChatResponseDto,
  UpdateSessionTitleDto
} from '../models/ai-chat.model';

export interface PaginationRequest {
  pageNumber: number;
  pageSize: number;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
}

@Injectable({
  providedIn: 'root'
})
export class AiChatApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/ai-chat`;

  createSession(dto: CreateChatSessionDto): Observable<ApiResponse<ChatSessionListDto>> {
    return this.http.post<ApiResponse<ChatSessionListDto>>(`${this.baseUrl}/sessions`, dto);
  }

  getSessions(params: PaginationRequest): Observable<ApiResponse<PagedResult<ChatSessionListDto>>> {
    return this.http.get<ApiResponse<PagedResult<ChatSessionListDto>>>(`${this.baseUrl}/sessions`, {
      params: {
        pageNumber: params.pageNumber.toString(),
        pageSize: params.pageSize.toString()
      }
    });
  }

  getSessionById(id: string): Observable<ApiResponse<ChatSessionDetailDto>> {
    return this.http.get<ApiResponse<ChatSessionDetailDto>>(`${this.baseUrl}/sessions/${id}`);
  }

  sendMessage(id: string, dto: SendChatMessageDto): Observable<ApiResponse<ChatResponseDto>> {
    // Use SKIP_ERROR_TOAST to prevent global error toasts for chat message failures.
    // The store will catch the error and display an inline error state for the user to retry.
    return this.http.post<ApiResponse<ChatResponseDto>>(`${this.baseUrl}/sessions/${id}/messages`, dto, {
      context: new HttpContext().set(SKIP_ERROR_TOAST, true)
    });
  }

  updateSessionTitle(id: string, dto: UpdateSessionTitleDto): Observable<ApiResponse<boolean>> {
    return this.http.put<ApiResponse<boolean>>(`${this.baseUrl}/sessions/${id}/title`, dto);
  }

  deleteSession(id: string): Observable<ApiResponse<boolean>> {
    return this.http.delete<ApiResponse<boolean>>(`${this.baseUrl}/sessions/${id}`);
  }
}
