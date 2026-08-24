export interface Warehouse {
  id: string;
  name: string;
  location: string;
  isActive: boolean;
  createdAtUtc: string;
  updatedAtUtc?: string | null;
}

export interface CreateWarehouseRequest {
  name: string;
  location: string;
  isActive?: boolean;
}

export interface UpdateWarehouseRequest {
  name: string;
  location: string;
  isActive: boolean;
}
