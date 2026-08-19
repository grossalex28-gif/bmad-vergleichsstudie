import { Routes } from '@angular/router';

import { CartView } from './features/cart/cart-view/cart-view';
import { ProductList } from './features/catalog/product-list/product-list';
import { CheckoutView } from './features/order/checkout-view/checkout-view';
import { OrderView } from './features/order/order-view/order-view';
import { ProductDetail } from './features/product/product-detail/product-detail';

export const routes: Routes = [
  { path: '', component: ProductList },
  { path: 'products/:id', component: ProductDetail },
  { path: 'cart', component: CartView },
  { path: 'checkout', component: CheckoutView },
  { path: 'orders/:id', component: OrderView }
];
