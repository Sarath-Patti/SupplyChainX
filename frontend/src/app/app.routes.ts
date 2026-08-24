import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
  {
    path: 'dashboard',
    loadComponent: () => import('./features/dashboard/dashboard.component').then(m => m.DashboardComponent)
  },
  {
    path: 'products',
    loadComponent: () => import('./features/products/products.component').then(m => m.ProductsComponent)
  },
  {
    path: 'warehouses',
    loadComponent: () => import('./features/warehouses/warehouses.component').then(m => m.WarehousesComponent)
  },
  {
    path: 'inventory',
    loadComponent: () => import('./features/inventory/inventory.component').then(m => m.InventoryComponent)
  },
  { path: '**', redirectTo: 'dashboard' }
];
