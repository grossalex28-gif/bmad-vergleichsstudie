import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { fehlermeldung } from '../../core/api-fehler';
import { VeranstaltungDetail } from '../../core/models/veranstaltung.model';
import { VeranstaltungenService } from '../../core/services/veranstaltungen.service';

@Component({
  selector: 'app-veranstaltungsdetail',
  imports: [RouterLink, DatePipe, DecimalPipe],
  templateUrl: './veranstaltungsdetail.html',
  styleUrl: './veranstaltungsdetail.scss'
})
export class VeranstaltungsdetailComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly veranstaltungenService = inject(VeranstaltungenService);

  protected readonly veranstaltung = signal<VeranstaltungDetail | null>(null);
  protected readonly ladeFehler = signal<string | null>(null);

  constructor() {
    const id = this.route.snapshot.paramMap.get('id')!;
    this.veranstaltungenService.getEine(id).subscribe({
      next: (daten) => this.veranstaltung.set(daten),
      error: (fehler) =>
        this.ladeFehler.set(fehlermeldung(fehler, 'Die Veranstaltung konnte nicht geladen werden.'))
    });
  }
}
