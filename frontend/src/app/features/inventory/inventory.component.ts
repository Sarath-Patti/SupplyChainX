import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { InventoryService } from '../../core/services/inventory.service';
import { ProductService } from '../../core/services/product.service';
import { WarehouseService } from '../../core/services/warehouse.service';
import { AuthService } from '../../core/services/auth.service';
import { Inventory, AdjustInventoryRequest, InventoryAdjustmentType } from '../../core/models/inventory.model';
import { Product } from '../../core/models/product.model';
import { Warehouse } from '../../core/models/warehouse.model';
import { PagedResult } from '../../core/models/common.model';

@Component({
  selector: 'app-inventory',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './inventory.component.html',
  styleUrl: './inventory.component.css'
})
export class InventoryComponent implements OnInit {
  pagedInventory: PagedResult<Inventory> | null = null;
  productsList: Product[] = [];
  warehousesList: Warehouse[] = [];

  isLoading = true;
  errorMessage: string | null = null;
  successMessage: string | null = null;

  currentPage = 1;
  pageSize = 10;
  selectedProductId: string | null = null;
  selectedWarehouseId: string | null = null;

  showAdjustModal = false;
  isSubmitting = false;

  adjustModel: AdjustInventoryRequest = {
    productId: '',
    warehouseId: '',
    quantity: 10,
    adjustmentType: InventoryAdjustmentType.Increase
  };

  AdjustmentType = InventoryAdjustmentType;

  constructor(
    private readonly inventoryService: InventoryService,
    private readonly productService: ProductService,
    private readonly warehouseService: WarehouseService,
    public readonly authService: AuthService
  ) {}

  ngOnInit(): void {
    this.loadDropdowns();
    this.loadInventory();
  }

  loadDropdowns(): void {
    this.productService.getProducts({ page: 1, pageSize: 100 }).subscribe({
      next: (res) => (this.productsList = res.items)
    });

    this.warehouseService.getWarehouses({ page: 1, pageSize: 100 }).subscribe({
      next: (res) => (this.warehousesList = res.items)
    });
  }

  loadInventory(): void {
    this.isLoading = true;
    this.errorMessage = null;

    this.inventoryService.getInventory(
      { page: this.currentPage, pageSize: this.pageSize },
      this.selectedProductId ?? undefined,
      this.selectedWarehouseId ?? undefined
    ).subscribe({
      next: (res) => {
        this.pagedInventory = res;
        this.isLoading = false;
      },
      error: (err) => {
        this.errorMessage = err.error?.detail || err.message || 'Failed to load inventory records';
        this.isLoading = false;
      }
    });
  }

  onFilterChange(): void {
    this.currentPage = 1;
    this.loadInventory();
  }

  changePage(newPage: number): void {
    if (newPage >= 1 && (!this.pagedInventory || newPage <= this.pagedInventory.totalPages)) {
      this.currentPage = newPage;
      this.loadInventory();
    }
  }

  openAdjustModal(item?: Inventory): void {
    if (!this.authService.canWrite()) return;
    this.adjustModel = {
      productId: item ? item.productId : (this.productsList.length > 0 ? this.productsList[0].id : ''),
      warehouseId: item ? item.warehouseId : (this.warehousesList.length > 0 ? this.warehousesList[0].id : ''),
      quantity: 10,
      adjustmentType: InventoryAdjustmentType.Increase
    };
    this.showAdjustModal = true;
  }

  closeAdjustModal(): void {
    this.showAdjustModal = false;
  }

  submitAdjust(): void {
    if (!this.adjustModel.productId || !this.adjustModel.warehouseId || this.adjustModel.quantity <= 0) {
      alert('Please select valid Product, Warehouse, and Quantity > 0.');
      return;
    }

    this.isSubmitting = true;
    this.adjustModel.adjustmentType = Number(this.adjustModel.adjustmentType);

    this.inventoryService.adjustInventory(this.adjustModel).subscribe({
      next: (updated) => {
        this.isSubmitting = false;
        this.showAdjustModal = false;
        this.triggerSuccess(`Inventory adjusted successfully! Available: ${updated.availableQuantity}, Reserved: ${updated.reservedQuantity}`);
        this.loadInventory();
      },
      error: (err) => {
        this.isSubmitting = false;
        alert(err.error?.detail || err.message || 'Failed to adjust inventory stock.');
      }
    });
  }

  getAdjustmentTypeName(type: InventoryAdjustmentType): string {
    switch (type) {
      case InventoryAdjustmentType.Increase: return 'Increase Stock (+)';
      case InventoryAdjustmentType.Decrease: return 'Decrease Stock (-)';
      case InventoryAdjustmentType.Reserve: return 'Reserve Stock (🔒)';
      case InventoryAdjustmentType.Release: return 'Release Reserved (🔓)';
      default: return 'Unknown';
    }
  }

  private triggerSuccess(msg: string): void {
    this.successMessage = msg;
    setTimeout(() => {
      this.successMessage = null;
    }, 4000);
  }
}
