import { Component, input } from '@angular/core';

@Component({
  selector: 'app-badge-status',
  imports: [],
  templateUrl: './badge-status.html',
  styleUrl: './badge-status.scss',
})
export class BadgeStatus {
  readonly status = input.required<'Aktiv' | 'Storniert'>();
}
