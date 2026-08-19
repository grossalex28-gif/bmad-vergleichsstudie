import { Injectable, signal } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class CartService {
  private readonly _cartId = signal<string | null>(null);
  readonly cartId = this._cartId.asReadonly();

  setCartId(id: string): void {
    this._cartId.set(id);
  }
}
