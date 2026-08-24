export interface Product {
  id: string;
  sku: string;
  name: string;
  description?: string;
  unitPrice: number;
  isActive: boolean;
  createdAtUtc: string;
  updatedAtUtc?: string | null;
}

export interface CreateProductRequest {
  sku: string;
  name: string;
  description?: string;
  unitPrice: number;
  isActive?: boolean;
}

export interface UpdateProductRequest {
  sku: string;
  name: string;
  description?: string;
  unitPrice: number;
  isActive: boolean;
}
