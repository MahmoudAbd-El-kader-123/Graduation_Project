import { Injectable, inject, signal } from '@angular/core';
import { Subject, switchMap, debounceTime, distinctUntilChanged, catchError, of, map, tap } from 'rxjs';
import { VendorService } from '../../../features/vendors/services/vendor.service';
import { VendorOption } from '../models/vendor-option.model';

@Injectable({
  providedIn: 'root'
})
export class VendorLookupService {
  private readonly vendorService = inject(VendorService);

  readonly loading = signal<boolean>(false);
  readonly vendors = signal<VendorOption[]>([]);
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
        return this.vendorService.getVendors(page, this.pageSize, query?.trim() || undefined).pipe(
          catchError(() => of(null))
        );
      })
    ).subscribe(response => {
      this.loading.set(false);
      if (response?.success && response.data) {
        const approvedVendors = response.data.items
          .filter(v => v.isApproved)
          .map(v => ({
            id: v.id,
            name: v.name,
            erpId: v.erpId
          }));
          
        if (this.currentPage === 1) {
          this.vendors.set(approvedVendors);
        } else {
          this.vendors.update(current => [...current, ...approvedVendors]);
        }
        
        // Since we filter on the frontend (isApproved), the totalCount might be slightly inaccurate.
        // But for infinite scrolling, checking if we received less than pageSize is a safe bet.
        this.hasMore.set(response.data.items.length === this.pageSize);
      } else if (this.currentPage === 1) {
        this.vendors.set([]);
        this.hasMore.set(false);
      }
    });
  }

  /**
   * Trigger a debounced search. Pass the user's current query string.
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
    this.vendors.set([]);
    this.hasMore.set(false);
    this.currentQuery = '';
    this.currentPage = 1;
  }
  
  /** Legacy method to prevent breaking existing usages during transition */
  loadVendors() {
    this.search('');
    return of([]);
  }
}
