import { Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router } from '@angular/router';
import { BuchungenService } from '../shared/api/buchungen.service';

@Component({
  selector: 'app-buchung-abrufen',
  imports: [],
  templateUrl: './buchung-abrufen.html',
  styleUrl: './buchung-abrufen.scss',
})
export class BuchungAbrufen {
  private readonly buchungenService = inject(BuchungenService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  readonly referenzEingabe = signal('');
  readonly laedt = signal(false);
  readonly fehler = signal<string | null>(null);

  protected absenden(): void {
    const referenz = this.referenzEingabe().trim();
    if (!referenz || this.laedt()) return;

    this.laedt.set(true);
    this.fehler.set(null);

    this.buchungenService
      .buchungAbrufen(referenz)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.laedt.set(false);
          this.router.navigate(['/buchungen', referenz]);
        },
        error: () => {
          this.laedt.set(false);
          this.fehler.set('Keine Buchung mit dieser Referenz gefunden.');
        },
      });
  }
}
