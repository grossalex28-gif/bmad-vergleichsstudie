import { DecimalPipe } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';

import { CartService } from '../../core/services/cart.service';

@Component({
  selector: 'app-cart',
  imports: [DecimalPipe, FormsModule, RouterLink],
  templateUrl: './cart.html',
  styleUrl: './cart.scss'
})
export class Cart {
  protected readonly cartService = inject(CartService);

  protected updateQuantity(produktId: string, lieferantId: string, menge: number): void {
    if (menge < 1) {
      return;
    }
    this.cartService.updateQuantity(produktId, lieferantId, menge);
  }

  protected removeItem(produktId: string, lieferantId: string): void {
    this.cartService.removeItem(produktId, lieferantId);
  }
}
