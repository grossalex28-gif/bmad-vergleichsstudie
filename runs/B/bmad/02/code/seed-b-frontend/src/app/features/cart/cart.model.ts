export interface CartItemLine {
  productId: string;
  productName: string;
  supplierId: string;
  supplierName: string;
  quantity: number;
  unitPrice: number;
  lineTotal: number;
}

export interface Cart {
  cartId: string | null;
  items: CartItemLine[];
  totalPrice: number;
}
