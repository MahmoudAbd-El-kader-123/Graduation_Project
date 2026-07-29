import { Injectable, inject, signal } from '@angular/core';
import { Subject, switchMap, debounceTime, distinctUntilChanged, catchError, of } from 'rxjs';
import { PurchaseOrderApiService } from '../../../features/purchase-orders/services/purchase-order-api.service';
import { PurchaseOrder } from '../../../features/purchase-orders/models/purchase-order.model';

/**
 * Provides debounced, server-side Purchase Order search for UI selectors.
 *
 * Architecture:
 *   PurchaseOrderLookupService
 *       └── PurchaseOrderApiService  (reused, not duplicated)
 *           └── HttpClient
 *
 * Usage:
 *   - Call `search(query)` from (completeMethod) of p-autocomplete.
 *   - Bind `suggestions()` and `loading()` signals to the template.
 *   - Call `clear()` when the dialog/form is reset.
 *
 * Data:
 *   - Returns at most 20 results per query (server-side, scalable).
 *   - Debounces 300ms; cancels in-flight requests via switchMap.
 *   - No client-side size limit — scales to any number of Purchase Orders.
 */
@Injectable({ providedIn: 'root' })
export class PurchaseOrderLookupService {
  private readonly api = inject(PurchaseOrderApiService);

  readonly loading   = signal<boolean>(false);
  readonly suggestions = signal<PurchaseOrder[]>([]);

  private readonly query$ = new Subject<string>();

  constructor() {
    this.query$.pipe(
      debounceTime(300),
      distinctUntilChanged(),
      switchMap(query => {
        this.loading.set(true);
        return this.api.getPurchaseOrders({
          searchQuery: query?.trim() || undefined,
          pageNumber:  1,
          pageSize:    20
        }).pipe(
          catchError(() => of(null))
        );
      })
    ).subscribe(response => {
      this.loading.set(false);
      if (response?.success && response.data) {
        this.suggestions.set(response.data.items ?? []);
      } else {
        this.suggestions.set([]);
      }
    });
  }

  /**
   * Trigger a debounced search. Pass the user's current query string.
   * Pass an empty string to load the first page of recent Purchase Orders.
   */
  search(query: string): void {
    this.query$.next(query ?? '');
  }

  /** Reset suggestions without making a network request. */
  clear(): void {
    this.suggestions.set([]);
  }
}
