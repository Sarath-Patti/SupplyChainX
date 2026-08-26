import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { WarehouseService } from '../../core/services/warehouse.service';
import { AuthService } from '../../core/services/auth.service';
import { Warehouse, CreateWarehouseRequest, UpdateWarehouseRequest } from '../../core/models/warehouse.model';
import { PagedResult } from '../../core/models/common.model';

@Component({
  selector: 'app-warehouses',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './warehouses.component.html',
  styleUrl: './warehouses.component.css'
})
export class WarehousesComponent implements OnInit {
  pagedWarehouses: PagedResult<Warehouse> | null = null;
  isLoading = true;
  errorMessage: string | null = null;
  successMessage: string | null = null;

  currentPage = 1;
  pageSize = 10;
  searchQuery = '';
  activeFilter: boolean | null = null;

  showCreateModal = false;
  showEditModal = false;
  showDeleteModal = false;

  createModel: CreateWarehouseRequest = {
    name: '',
    location: '',
    isActive: true
  };

  editModel: UpdateWarehouseRequest = {
    name: '',
    location: '',
    isActive: true
  };

  selectedWarehouse: Warehouse | null = null;
  isSubmitting = false;

  constructor(
    private readonly warehouseService: WarehouseService,
    public readonly authService: AuthService
  ) {}

  ngOnInit(): void {
    this.loadWarehouses();
  }

  loadWarehouses(): void {
    this.isLoading = true;
    this.errorMessage = null;

    this.warehouseService.getWarehouses(
      { page: this.currentPage, pageSize: this.pageSize, search: this.searchQuery },
      this.activeFilter ?? undefined
    ).subscribe({
      next: (res) => {
        this.pagedWarehouses = res;
        this.isLoading = false;
      },
      error: (err) => {
        this.errorMessage = err.error?.detail || err.message || 'Failed to load warehouses';
        this.isLoading = false;
      }
    });
  }

  onSearchChange(): void {
    this.currentPage = 1;
    this.loadWarehouses();
  }

  onFilterChange(val: string): void {
    if (val === 'active') this.activeFilter = true;
    else if (val === 'inactive') this.activeFilter = false;
    else this.activeFilter = null;

    this.currentPage = 1;
    this.loadWarehouses();
  }

  changePage(newPage: number): void {
    if (newPage >= 1 && (!this.pagedWarehouses || newPage <= this.pagedWarehouses.totalPages)) {
      this.currentPage = newPage;
      this.loadWarehouses();
    }
  }

  openCreateModal(): void {
    if (!this.authService.canWrite()) return;
    this.createModel = { name: '', location: '', isActive: true };
    this.showCreateModal = true;
  }

  closeCreateModal(): void {
    this.showCreateModal = false;
  }

  submitCreate(): void {
    if (!this.createModel.name || !this.createModel.location) return;

    this.isSubmitting = true;
    this.warehouseService.createWarehouse(this.createModel).subscribe({
      next: (created) => {
        this.isSubmitting = false;
        this.showCreateModal = false;
        this.triggerSuccess(`Warehouse '${created.name}' created successfully!`);
        this.loadWarehouses();
      },
      error: (err) => {
        this.isSubmitting = false;
        alert(err.error?.detail || err.message || 'Failed to create warehouse');
      }
    });
  }

  openEditModal(warehouse: Warehouse): void {
    if (!this.authService.canWrite()) return;
    this.selectedWarehouse = warehouse;
    this.editModel = {
      name: warehouse.name,
      location: warehouse.location,
      isActive: warehouse.isActive
    };
    this.showEditModal = true;
  }

  closeEditModal(): void {
    this.showEditModal = false;
    this.selectedWarehouse = null;
  }

  submitEdit(): void {
    if (!this.selectedWarehouse || !this.editModel.name || !this.editModel.location) return;

    this.isSubmitting = true;
    this.warehouseService.updateWarehouse(this.selectedWarehouse.id, this.editModel).subscribe({
      next: (updated) => {
        this.isSubmitting = false;
        this.showEditModal = false;
        this.triggerSuccess(`Warehouse '${updated.name}' updated successfully!`);
        this.loadWarehouses();
      },
      error: (err) => {
        this.isSubmitting = false;
        alert(err.error?.detail || err.message || 'Failed to update warehouse');
      }
    });
  }

  openDeleteModal(warehouse: Warehouse): void {
    if (!this.authService.canWrite()) return;
    this.selectedWarehouse = warehouse;
    this.showDeleteModal = true;
  }

  closeDeleteModal(): void {
    this.showDeleteModal = false;
    this.selectedWarehouse = null;
  }

  confirmDelete(): void {
    if (!this.selectedWarehouse) return;

    this.isSubmitting = true;
    this.warehouseService.deleteWarehouse(this.selectedWarehouse.id).subscribe({
      next: () => {
        this.isSubmitting = false;
        this.showDeleteModal = false;
        this.triggerSuccess(`Warehouse '${this.selectedWarehouse?.name}' deleted.`);
        this.selectedWarehouse = null;
        this.loadWarehouses();
      },
      error: (err) => {
        this.isSubmitting = false;
        alert(err.error?.detail || err.message || 'Failed to delete warehouse');
      }
    });
  }

  private triggerSuccess(msg: string): void {
    this.successMessage = msg;
    setTimeout(() => {
      this.successMessage = null;
    }, 4000);
  }
}
