export interface CreateOrderItemRequest {
  productId: string;
  supplierId: string;
  quantity: number;
}

export interface CreateOrderRequest {
  contactName: string;
  email: string;
  street: string;
  postalCode: string;
  city: string;
  items: CreateOrderItemRequest[];
}

export interface OrderItem {
  productId: string;
  productName: string;
  supplierId: string;
  supplierName: string;
  unitPrice: number;
  quantity: number;
  lineTotal: number;
}

export interface Order {
  id: number;
  status: string;
  createdAt: string;
  contactName: string;
  email: string;
  street: string;
  postalCode: string;
  city: string;
  items: OrderItem[];
  total: number;
}

export interface ApiProblem {
  title?: string;
  detail?: string;
  errors?: Record<string, string[]>;
}
