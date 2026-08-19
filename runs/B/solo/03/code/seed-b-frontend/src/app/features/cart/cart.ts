import { DecimalPipe } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { CartService } from '../../core/services/cart.service';

@Component({
  selector: 'app-cart',
  imports: [FormsModule, RouterLink, DecimalPipe],
  templateUrl: './cart.html',
  styleUrl: './cart.scss',
})
export class Cart {
  protected readonly cart = inject(CartService);

  protected updateQuantity(productId: string, supplierId: string, quantity: number): void {
    this.cart.updateQuantity(productId, supplierId, quantity);
  }

  protected removeItem(productId: string, supplierId: string): void {
    this.cart.removeItem(productId, supplierId);
  }
}
