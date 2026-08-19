import { CartItem } from './cart.model';

export interface CreateOrderRequest {
  name: string;
  street: string;
  postalCode: string;
  city: string;
  country: string;
  email: string;
  items: CartItem[];
}

export interface OrderCreated {
  orderId: string;
}

export interface OrderItem {
  productName: string;
  supplierName: string;
  unitPrice: number;
  quantity: number;
}

export interface OrderDetail {
  id: string;
  status: string;
  items: OrderItem[];
  totalAmount: number;
}
