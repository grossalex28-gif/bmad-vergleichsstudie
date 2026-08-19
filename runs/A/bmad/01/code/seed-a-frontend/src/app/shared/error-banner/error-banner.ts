import { Component, input } from '@angular/core';

@Component({
  selector: 'app-error-banner',
  imports: [],
  templateUrl: './error-banner.html',
  styleUrl: './error-banner.scss'
})
export class ErrorBanner {
  readonly message = input.required<string>();
}
