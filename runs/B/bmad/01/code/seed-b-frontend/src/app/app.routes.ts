import { Routes } from '@angular/router';

import { Cart } from './features/cart/cart';
import { Catalog } from './features/catalog/catalog';
import { Checkout } from './features/checkout/checkout';
import { OrderConfirmation } from './features/order/order-confirmation';
import { ProductDetail } from './features/product-detail/product-detail';

export const routes: Routes = [
  { path: '', component: Catalog },
  { path: 'products/:id', component: ProductDetail },
  { path: 'cart', component: Cart },
  { path: 'checkout', component: Checkout },
  { path: 'orders/:publicId', component: OrderConfirmation },
];
