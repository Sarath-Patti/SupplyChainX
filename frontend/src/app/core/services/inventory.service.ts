import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Inventory, AdjustInventoryRequest } from '../models/inventory.model';
import { PagedResult, PaginationParams } from '../models/common.model';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class InventoryService {
  private readonly baseUrl = `${environment.apiBaseUrl}/api/v1/inventory`;

  constructor(private readonly http: HttpClient) {}

  getInventory(params?: PaginationParams, productId?: string, warehouseId?: string): Observable<PagedResult<Inventory>> {
    let httpParams = new HttpParams();
    if (params?.page) httpParams = httpParams.set('page', params.page.toString());
    if (params?.pageSize) httpParams = httpParams.set('pageSize', params.pageSize.toString());
    if (productId) httpParams = httpParams.set('productId', productId);
    if (warehouseId) httpParams = httpParams.set('warehouseId', warehouseId);

    return this.http.get<PagedResult<Inventory>>(this.baseUrl, { params: httpParams });
  }

  getInventoryByProductAndWarehouse(productId: string, warehouseId: string): Observable<Inventory> {
    return this.http.get<Inventory>(`${this.baseUrl}/${productId}/${warehouseId}`);
  }

  adjustInventory(request: AdjustInventoryRequest): Observable<Inventory> {
    return this.http.post<Inventory>(`${this.baseUrl}/adjust`, request);
  }
}
