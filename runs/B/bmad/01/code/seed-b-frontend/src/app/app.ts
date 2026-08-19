import { Component, computed, inject } from '@angular/core';
import { RouterLink, RouterOutlet } from '@angular/router';

import { CartService } from './features/cart/cart.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App {
  private readonly cartService = inject(CartService);

  protected readonly cartItemCount = computed(() =>
    this.cartService.items().reduce((sum, item) => sum + item.quantity, 0),
  );
}
