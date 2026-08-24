export enum InventoryAdjustmentType {
  Increase = 1,
  Decrease = 2,
  Reserve = 3,
  Release = 4
}

export interface Inventory {
  id: string;
  productId: string;
  productSku?: string;
  productName?: string;
  warehouseId: string;
  warehouseName?: string;
  availableQuantity: number;
  reservedQuantity: number;
  minimumStockThreshold: number;
  version: number;
  createdAtUtc: string;
  updatedAtUtc?: string | null;
}

export interface AdjustInventoryRequest {
  productId: string;
  warehouseId: string;
  quantity: number;
  adjustmentType: InventoryAdjustmentType;
}
