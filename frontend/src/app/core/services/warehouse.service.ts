import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Warehouse, CreateWarehouseRequest, UpdateWarehouseRequest } from '../models/warehouse.model';
import { PagedResult, PaginationParams } from '../models/common.model';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class WarehouseService {
  private readonly baseUrl = `${environment.apiBaseUrl}/api/v1/warehouses`;

  constructor(private readonly http: HttpClient) {}

  getWarehouses(params?: PaginationParams, isActive?: boolean): Observable<PagedResult<Warehouse>> {
    let httpParams = new HttpParams();
    if (params?.page) httpParams = httpParams.set('page', params.page.toString());
    if (params?.pageSize) httpParams = httpParams.set('pageSize', params.pageSize.toString());
    if (params?.search) httpParams = httpParams.set('search', params.search);
    if (isActive !== undefined && isActive !== null) httpParams = httpParams.set('isActive', isActive.toString());

    return this.http.get<PagedResult<Warehouse>>(this.baseUrl, { params: httpParams });
  }

  getWarehouseById(id: string): Observable<Warehouse> {
    return this.http.get<Warehouse>(`${this.baseUrl}/${id}`);
  }

  createWarehouse(request: CreateWarehouseRequest): Observable<Warehouse> {
    return this.http.post<Warehouse>(this.baseUrl, request);
  }

  updateWarehouse(id: string, request: UpdateWarehouseRequest): Observable<Warehouse> {
    return this.http.put<Warehouse>(`${this.baseUrl}/${id}`, request);
  }

  deleteWarehouse(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}
