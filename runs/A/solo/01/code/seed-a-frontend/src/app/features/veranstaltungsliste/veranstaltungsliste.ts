import { DatePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { fehlermeldung } from '../../core/api-fehler';
import { Spielstaette } from '../../core/models/spielstaette.model';
import { VeranstaltungListItem } from '../../core/models/veranstaltung.model';
import { SpielstaettenService } from '../../core/services/spielstaetten.service';
import { VeranstaltungenService } from '../../core/services/veranstaltungen.service';

@Component({
  selector: 'app-veranstaltungsliste',
  imports: [FormsModule, RouterLink, DatePipe],
  templateUrl: './veranstaltungsliste.html',
  styleUrl: './veranstaltungsliste.scss'
})
export class VeranstaltungslisteComponent {
  private readonly veranstaltungenService = inject(VeranstaltungenService);
  private readonly spielstaettenService = inject(SpielstaettenService);

  protected readonly veranstaltungen = signal<VeranstaltungListItem[]>([]);
  protected readonly spielstaetten = signal<Spielstaette[]>([]);
  protected readonly ladeFehler = signal<string | null>(null);
  protected readonly laedt = signal(false);

  protected von = '';
  protected bis = '';
  protected spielstaetteId = '';

  constructor() {
    this.spielstaettenService.getAlle().subscribe({
      next: (daten) => this.spielstaetten.set(daten)
    });
    this.suchen();
  }

  protected suchen(): void {
    this.laedt.set(true);
    this.ladeFehler.set(null);

    this.veranstaltungenService
      .getAlle({
        von: this.von || undefined,
        bis: this.bis || undefined,
        spielstaetteId: this.spielstaetteId || undefined
      })
      .subscribe({
        next: (daten) => {
          this.veranstaltungen.set(daten);
          this.laedt.set(false);
        },
        error: (fehler) => {
          this.ladeFehler.set(fehlermeldung(fehler, 'Die Veranstaltungen konnten nicht geladen werden.'));
          this.laedt.set(false);
        }
      });
  }

  protected filterZuruecksetzen(): void {
    this.von = '';
    this.bis = '';
    this.spielstaetteId = '';
    this.suchen();
  }
}
