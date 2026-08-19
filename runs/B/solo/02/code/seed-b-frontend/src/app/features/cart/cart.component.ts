import { Component, inject } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { CartService } from '../../core/services/cart.service';

@Component({
  selector: 'app-cart',
  imports: [DecimalPipe, FormsModule, RouterLink],
  templateUrl: './cart.component.html',
  styleUrl: './cart.component.scss',
})
export class CartComponent {
  protected readonly cart = inject(CartService);

  updateQuantity(productId: number, supplierId: number, quantity: number): void {
    this.cart.updateQuantity(productId, supplierId, Number(quantity));
  }

  remove(productId: number, supplierId: number): void {
    this.cart.removeItem(productId, supplierId);
  }
}
