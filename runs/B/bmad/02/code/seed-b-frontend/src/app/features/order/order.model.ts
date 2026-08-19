export interface OrderItemLine {
  productId: string;
  productName: string;
  supplierId: string;
  supplierName: string;
  quantity: number;
  unitPrice: number;
  lineTotal: number;
}

export interface Order {
  orderId: string;
  status: string;
  items: OrderItemLine[];
  totalPrice: number;
}

export interface OrderRejectionLine {
  productId: string;
  supplierId: string;
  reason: string;
}

export interface OrderRejection {
  error: string;
  lines: OrderRejectionLine[];
}
