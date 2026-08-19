import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { BuchungenService } from '../../core/services/buchungen.service';
import { ApiProblem, Buchung } from '../../core/models/api.models';

@Component({
  selector: 'app-buchung-anzeige',
  imports: [CommonModule, RouterLink],
  templateUrl: './buchung-anzeige.html',
  styleUrl: './buchung-anzeige.scss'
})
export class BuchungAnzeige {
  private readonly route = inject(ActivatedRoute);
  private readonly buchungenService = inject(BuchungenService);

  readonly buchung = signal<Buchung | null>(null);
  readonly loading = signal(true);
  readonly fehler = signal<string | null>(null);

  readonly wirdStorniert = signal(false);
  readonly stornierenFehler = signal<string | null>(null);

  constructor() {
    this.laden();
  }

  private laden(): void {
    const referenz = this.route.snapshot.paramMap.get('referenz')!;
    this.loading.set(true);
    this.fehler.set(null);

    this.buchungenService.getByReferenz(referenz).subscribe({
      next: (buchung) => {
        this.buchung.set(buchung);
        this.loading.set(false);
      },
      error: (fehler: HttpErrorResponse) => {
        const problem = fehler.error as ApiProblem | undefined;
        this.fehler.set(
          fehler.status === 404
            ? 'Es wurde keine Buchung mit dieser Referenz gefunden.'
            : (problem?.detail ?? 'Die Buchung konnte nicht geladen werden.')
        );
        this.loading.set(false);
      }
    });
  }

  stornieren(): void {
    const buchung = this.buchung();
    if (!buchung || !confirm('Möchten Sie diese Buchung wirklich stornieren?')) {
      return;
    }

    this.wirdStorniert.set(true);
    this.stornierenFehler.set(null);

    this.buchungenService.stornieren(buchung.referenz).subscribe({
      next: (aktualisiert) => {
        this.buchung.set(aktualisiert);
        this.wirdStorniert.set(false);
      },
      error: (fehler: HttpErrorResponse) => {
        const problem = fehler.error as ApiProblem | undefined;
        this.stornierenFehler.set(problem?.detail ?? 'Die Buchung konnte nicht storniert werden.');
        this.wirdStorniert.set(false);
      }
    });
  }
}
