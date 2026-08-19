import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./features/catalog/product-list/product-list').then((m) => m.ProductList)
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
    loadComponent: () =>
      import('./features/checkout/order-confirmation/order-confirmation').then((m) => m.OrderConfirmation)
  }
];
