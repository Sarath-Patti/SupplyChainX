import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ProductService } from '../../core/services/product.service';
import { AuthService } from '../../core/services/auth.service';
import { Product, CreateProductRequest, UpdateProductRequest } from '../../core/models/product.model';
import { PagedResult } from '../../core/models/common.model';

@Component({
  selector: 'app-products',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './products.component.html',
  styleUrl: './products.component.css'
})
export class ProductsComponent implements OnInit {
  pagedProducts: PagedResult<Product> | null = null;
  isLoading = true;
  errorMessage: string | null = null;
  successMessage: string | null = null;

  // Query Params
  currentPage = 1;
  pageSize = 10;
  searchQuery = '';
  activeFilter: boolean | null = null;

  // Modals state
  showCreateModal = false;
  showEditModal = false;
  showDeleteModal = false;

  // Form Models
  createModel: CreateProductRequest = {
    sku: '',
    name: '',
    description: '',
    unitPrice: 0.0,
    isActive: true
  };

  editModel: UpdateProductRequest = {
    sku: '',
    name: '',
    description: '',
    unitPrice: 0.0,
    isActive: true
  };

  selectedProduct: Product | null = null;
  isSubmitting = false;

  constructor(
    private readonly productService: ProductService,
    public readonly authService: AuthService
  ) {}

  ngOnInit(): void {
    this.loadProducts();
  }

  loadProducts(): void {
    this.isLoading = true;
    this.errorMessage = null;

    this.productService.getProducts(
      { page: this.currentPage, pageSize: this.pageSize, search: this.searchQuery },
      this.activeFilter ?? undefined
    ).subscribe({
      next: (res) => {
        this.pagedProducts = res;
        this.isLoading = false;
      },
      error: (err) => {
        this.errorMessage = err.error?.detail || err.message || 'Failed to load products from server';
        this.isLoading = false;
      }
    });
  }

  onSearchChange(): void {
    this.currentPage = 1;
    this.loadProducts();
  }

  onFilterChange(val: string): void {
    if (val === 'active') this.activeFilter = true;
    else if (val === 'inactive') this.activeFilter = false;
    else this.activeFilter = null;

    this.currentPage = 1;
    this.loadProducts();
  }

  changePage(newPage: number): void {
    if (newPage >= 1 && (!this.pagedProducts || newPage <= this.pagedProducts.totalPages)) {
      this.currentPage = newPage;
      this.loadProducts();
    }
  }

  // Create Modal Actions
  openCreateModal(): void {
    if (!this.authService.canWrite()) return;
    this.createModel = {
      sku: '',
      name: '',
      description: '',
      unitPrice: 10.0,
      isActive: true
    };
    this.showCreateModal = true;
  }

  closeCreateModal(): void {
    this.showCreateModal = false;
  }

  submitCreate(): void {
    if (!this.createModel.sku || !this.createModel.name || this.createModel.unitPrice < 0) {
      return;
    }

    this.isSubmitting = true;
    this.productService.createProduct(this.createModel).subscribe({
      next: (created) => {
        this.isSubmitting = false;
        this.showCreateModal = false;
        this.triggerSuccess(`Product '${created.name}' (SKU: ${created.sku}) created successfully!`);
        this.loadProducts();
      },
      error: (err) => {
        this.isSubmitting = false;
        alert(err.error?.detail || err.message || 'Failed to create product');
      }
    });
  }

  // Edit Modal Actions
  openEditModal(product: Product): void {
    if (!this.authService.canWrite()) return;
    this.selectedProduct = product;
    this.editModel = {
      sku: product.sku,
      name: product.name,
      description: product.description || '',
      unitPrice: product.unitPrice,
      isActive: product.isActive
    };
    this.showEditModal = true;
  }

  closeEditModal(): void {
    this.showEditModal = false;
    this.selectedProduct = null;
  }

  submitEdit(): void {
    if (!this.selectedProduct || !this.editModel.sku || !this.editModel.name) {
      return;
    }

    this.isSubmitting = true;
    this.productService.updateProduct(this.selectedProduct.id, this.editModel).subscribe({
      next: (updated) => {
        this.isSubmitting = false;
        this.showEditModal = false;
        this.triggerSuccess(`Product '${updated.name}' updated successfully!`);
        this.loadProducts();
      },
      error: (err) => {
        this.isSubmitting = false;
        alert(err.error?.detail || err.message || 'Failed to update product');
      }
    });
  }

  // Delete Modal Actions
  openDeleteModal(product: Product): void {
    if (!this.authService.canWrite()) return;
    this.selectedProduct = product;
    this.showDeleteModal = true;
  }

  closeDeleteModal(): void {
    this.showDeleteModal = false;
    this.selectedProduct = null;
  }

  confirmDelete(): void {
    if (!this.selectedProduct) return;

    this.isSubmitting = true;
    this.productService.deleteProduct(this.selectedProduct.id).subscribe({
      next: () => {
        this.isSubmitting = false;
        this.showDeleteModal = false;
        this.triggerSuccess(`Product '${this.selectedProduct?.name}' deleted.`);
        this.selectedProduct = null;
        this.loadProducts();
      },
      error: (err) => {
        this.isSubmitting = false;
        alert(err.error?.detail || err.message || 'Failed to delete product');
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
