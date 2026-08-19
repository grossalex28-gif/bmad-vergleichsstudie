import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', redirectTo: 'products', pathMatch: 'full' },
  {
    path: 'products',
    loadComponent: () => import('./features/product-list/product-list').then((m) => m.ProductList)
  },
  {
    path: 'products/:id',
    loadComponent: () => import('./features/product-detail/product-detail').then((m) => m.ProductDetail)
  },
  {
    path: 'cart',
    loadComponent: () => import('./features/cart/cart').then((m) => m.Cart)
  },
  {
    path: 'checkout',
    loadComponent: () => import('./features/checkout/checkout').then((m) => m.Checkout)
  },
  {
    path: 'orders/:id',
    loadComponent: () => import('./features/order-detail/order-detail').then((m) => m.OrderDetail)
  },
  { path: '**', redirectTo: 'products' }
];
