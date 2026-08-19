import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', redirectTo: 'produkte', pathMatch: 'full' },
  {
    path: 'produkte',
    loadComponent: () => import('./features/product-list/product-list').then((m) => m.ProductList),
  },
  {
    path: 'produkte/:id',
    loadComponent: () => import('./features/product-detail/product-detail').then((m) => m.ProductDetail),
  },
  {
    path: 'warenkorb',
    loadComponent: () => import('./features/cart/cart').then((m) => m.Cart),
  },
  {
    path: 'kasse',
    loadComponent: () => import('./features/checkout/checkout').then((m) => m.Checkout),
  },
  {
    path: 'bestellungen/:id',
    loadComponent: () =>
      import('./features/order-confirmation/order-confirmation').then((m) => m.OrderConfirmation),
  },
  { path: '**', redirectTo: 'produkte' },
];
