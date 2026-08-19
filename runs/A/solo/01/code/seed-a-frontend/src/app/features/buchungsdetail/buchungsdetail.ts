import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { fehlermeldung } from '../../core/api-fehler';
import { Buchung } from '../../core/models/buchung.model';
import { BuchungenService } from '../../core/services/buchungen.service';

@Component({
  selector: 'app-buchungsdetail',
  imports: [RouterLink, DatePipe, DecimalPipe],
  templateUrl: './buchungsdetail.html',
  styleUrl: './buchungsdetail.scss'
})
export class BuchungsdetailComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly buchungenService = inject(BuchungenService);

  protected readonly referenz = this.route.snapshot.paramMap.get('referenz')!;
  protected readonly buchung = signal<Buchung | null>(null);
  protected readonly ladeFehler = signal<string | null>(null);
  protected readonly stornierenFehler = signal<string | null>(null);
  protected readonly wirdStorniert = signal(false);

  constructor() {
    this.laden();
  }

  private laden(): void {
    this.buchungenService.holeMitReferenz(this.referenz).subscribe({
      next: (buchung) => this.buchung.set(buchung),
      error: (fehler) =>
        this.ladeFehler.set(fehlermeldung(fehler, 'Die Buchung konnte nicht geladen werden.'))
    });
  }

  protected stornieren(): void {
    if (!confirm('Möchten Sie diese Buchung wirklich stornieren?')) {
      return;
    }

    this.wirdStorniert.set(true);
    this.stornierenFehler.set(null);

    this.buchungenService.stornieren(this.referenz).subscribe({
      next: (buchung) => {
        this.buchung.set(buchung);
        this.wirdStorniert.set(false);
      },
      error: (fehler) => {
        this.wirdStorniert.set(false);
        this.stornierenFehler.set(fehlermeldung(fehler, 'Die Buchung konnte nicht storniert werden.'));
      }
    });
  }
}
