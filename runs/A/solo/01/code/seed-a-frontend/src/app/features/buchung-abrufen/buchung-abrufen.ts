import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';

@Component({
  selector: 'app-buchung-abrufen',
  imports: [FormsModule],
  templateUrl: './buchung-abrufen.html',
  styleUrl: './buchung-abrufen.scss'
})
export class BuchungAbrufenComponent {
  private readonly router = inject(Router);

  protected referenz = '';
  protected readonly fehler = signal<string | null>(null);

  protected abrufen(): void {
    const referenz = this.referenz.trim().toUpperCase();
    if (!referenz) {
      this.fehler.set('Bitte geben Sie eine Buchungsreferenz ein.');
      return;
    }

    this.fehler.set(null);
    this.router.navigate(['/buchungen', referenz]);
  }
}
