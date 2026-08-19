import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { VeranstaltungenService } from '../../core/services/veranstaltungen.service';
import { Spielstaette, VeranstaltungListItem } from '../../core/models/api.models';

@Component({
  selector: 'app-veranstaltungen-liste',
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './veranstaltungen-liste.html',
  styleUrl: './veranstaltungen-liste.scss'
})
export class VeranstaltungenListe {
  private readonly veranstaltungenService = inject(VeranstaltungenService);

  readonly spielstaetten = signal<Spielstaette[]>([]);
  readonly veranstaltungen = signal<VeranstaltungListItem[]>([]);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  von = '';
  bis = '';
  spielstaetteId = '';

  constructor() {
    this.veranstaltungenService.getSpielstaetten().subscribe({
      next: (spielstaetten) => this.spielstaetten.set(spielstaetten)
    });
    this.ladeVeranstaltungen();
  }

  filtern(): void {
    this.ladeVeranstaltungen();
  }

  zuruecksetzen(): void {
    this.von = '';
    this.bis = '';
    this.spielstaetteId = '';
    this.ladeVeranstaltungen();
  }

  private ladeVeranstaltungen(): void {
    this.loading.set(true);
    this.error.set(null);
    this.veranstaltungenService
      .getVeranstaltungen({
        von: this.von || undefined,
        bis: this.bis || undefined,
        spielstaetteId: this.spielstaetteId || undefined
      })
      .subscribe({
        next: (veranstaltungen) => {
          this.veranstaltungen.set(veranstaltungen);
          this.loading.set(false);
        },
        error: () => {
          this.error.set('Die Veranstaltungen konnten nicht geladen werden.');
          this.loading.set(false);
        }
      });
  }
}
