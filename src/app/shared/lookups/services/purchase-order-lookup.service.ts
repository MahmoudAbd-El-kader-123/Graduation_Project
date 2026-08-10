import { Injectable, inject, signal } from '@angular/core';
import { Subject, switchMap, debounceTime, distinctUntilChanged, catchError, of } from 'rxjs';
import { PurchaseOrderApiService } from '../../../features/purchase-orders/services/purchase-order-api.service';
import { PurchaseOrder } from '../../../features/purchase-orders/models/purchase-order.model';

@Injectable({ providedIn: 'root' })
export class PurchaseOrderLookupService {
  private readonly api = inject(PurchaseOrderApiService);

  readonly loading = signal<boolean>(false);
  readonly suggestions = signal<PurchaseOrder[]>([]);
  readonly hasMore = signal<boolean>(false);

  private readonly query$ = new Subject<{ query: string, page: number }>();
  
  private currentQuery = '';
  private currentPage = 1;
  private readonly pageSize = 20;

  constructor() {
    this.query$.pipe(
      debounceTime(300),
      distinctUntilChanged((prev, curr) => prev.query === curr.query && prev.page === curr.page),
      switchMap(({ query, page }) => {
        this.loading.set(true);
        return this.api.getPurchaseOrders({
          searchQuery: query?.trim() || undefined,
          pageNumber:  page,
          pageSize:    this.pageSize
        }).pipe(
          catchError(() => of(null))
        );
      })
    ).subscribe(response => {
      this.loading.set(false);
      if (response?.success && response.data) {
        const items = response.data.items ?? [];
        if (this.currentPage === 1) {
          this.suggestions.set(items);
        } else {
          this.suggestions.update(current => [...current, ...items]);
        }
        
        this.hasMore.set(items.length === this.pageSize);
      } else if (this.currentPage === 1) {
        this.suggestions.set([]);
        this.hasMore.set(false);
      }
    });
  }

  /**
   * Trigger a debounced search. Pass the user's current query string.
   * Pass an empty string to load the first page of recent Purchase Orders.
   */
  search(query: string): void {
    this.currentQuery = query ?? '';
    this.currentPage = 1;
    this.query$.next({ query: this.currentQuery, page: this.currentPage });
  }

  /**
   * Load the next page of results for the current search query.
   */
  loadMore(): void {
    if (!this.loading() && this.hasMore()) {
      this.currentPage++;
      this.query$.next({ query: this.currentQuery, page: this.currentPage });
    }
  }

  /** Reset suggestions without making a network request. */
  clear(): void {
    this.suggestions.set([]);
    this.hasMore.set(false);
    this.currentQuery = '';
    this.currentPage = 1;
  }
}
