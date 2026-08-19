export interface OrderContact {
  recipientName: string;
  street: string;
  postalCode: string;
  city: string;
  email: string;
}

export interface OrderItemCreate {
  productId: number;
  supplierId: number;
  quantity: number;
}

export interface OrderCreate {
  contact: OrderContact;
  items: OrderItemCreate[];
}

export interface OrderItemResult {
  productId: number;
  productName: string;
  supplierId: number;
  supplierName: string;
  unitPrice: number;
  quantity: number;
  lineTotal: number;
}

export interface Order {
  id: number;
  status: string;
  createdAt: string;
  contact: OrderContact;
  items: OrderItemResult[];
  total: number;
}

export interface OrderValidationError {
  code: string;
  message: string;
  itemIndex: number | null;
}

export interface OrderValidationErrorResponse {
  errors: OrderValidationError[];
}
