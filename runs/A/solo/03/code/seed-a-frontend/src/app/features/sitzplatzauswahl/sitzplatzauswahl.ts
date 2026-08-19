import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { BuchungenService } from '../../core/services/buchungen.service';
import { VeranstaltungenService } from '../../core/services/veranstaltungen.service';
import { ApiProblem, Sitzplan, Sitzplatz, VeranstaltungDetail } from '../../core/models/api.models';

interface AusgewaehlterSitzplatz {
  reihe: string;
  spalte: number;
  preiskategorieId: string;
}

function sitzplatzSchluessel(reihe: string, spalte: number): string {
  return `${reihe}-${spalte}`;
}

@Component({
  selector: 'app-sitzplatzauswahl',
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './sitzplatzauswahl.html',
  styleUrl: './sitzplatzauswahl.scss'
})
export class Sitzplatzauswahl {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly veranstaltungenService = inject(VeranstaltungenService);
  private readonly buchungenService = inject(BuchungenService);

  protected readonly veranstaltungId = this.route.snapshot.paramMap.get('id')!;

  readonly veranstaltung = signal<VeranstaltungDetail | null>(null);
  readonly sitzplan = signal<Sitzplan | null>(null);
  readonly loading = signal(true);
  readonly ladeFehler = signal<string | null>(null);

  readonly auswahl = signal<Map<string, AusgewaehlterSitzplatz>>(new Map());

  name = '';
  email = '';
  readonly wirdGebucht = signal(false);
  readonly buchungsFehler = signal<string | null>(null);

  readonly sitzplatzZeilen = computed(() => {
    const plan = this.sitzplan();
    if (!plan) {
      return [];
    }
    return plan.raum.reihen.map((reihe) => ({
      reihe,
      plaetze: plan.sitzplaetze.filter((s) => s.reihe === reihe)
    }));
  });

  readonly ausgewaehlteListe = computed(() =>
    Array.from(this.auswahl().values()).sort((a, b) => (a.reihe === b.reihe ? a.spalte - b.spalte : a.reihe.localeCompare(b.reihe)))
  );

  readonly gesamtpreis = computed(() => {
    const plan = this.sitzplan();
    if (!plan) {
      return 0;
    }
    const preise = new Map(plan.preiskategorien.map((p) => [p.id, p.preis]));
    return this.ausgewaehlteListe().reduce((summe, s) => summe + (preise.get(s.preiskategorieId) ?? 0), 0);
  });

  constructor() {
    this.ladeDaten();
  }

  private ladeDaten(): void {
    this.loading.set(true);
    this.ladeFehler.set(null);

    this.veranstaltungenService.getDetail(this.veranstaltungId).subscribe({
      next: (veranstaltung) => this.veranstaltung.set(veranstaltung)
    });

    this.veranstaltungenService.getSitzplan(this.veranstaltungId).subscribe({
      next: (sitzplan) => {
        this.sitzplan.set(sitzplan);
        this.loading.set(false);
      },
      error: () => {
        this.ladeFehler.set('Der Sitzplan konnte nicht geladen werden.');
        this.loading.set(false);
      }
    });
  }

  istAusgewaehlt(sitzplatz: Sitzplatz): boolean {
    return this.auswahl().has(sitzplatzSchluessel(sitzplatz.reihe, sitzplatz.spalte));
  }

  sitzplatzKlasse(sitzplatz: Sitzplatz): string {
    if (sitzplatz.typ === 'Gang') {
      return 'sitz sitz--gang';
    }
    if (sitzplatz.status === 'Belegt') {
      return 'sitz sitz--belegt';
    }
    if (this.istAusgewaehlt(sitzplatz)) {
      return 'sitz sitz--ausgewaehlt';
    }
    return 'sitz sitz--frei';
  }

  sitzplatzKlicken(sitzplatz: Sitzplatz): void {
    if (sitzplatz.typ === 'Gang' || sitzplatz.status === 'Belegt') {
      return;
    }

    const schluessel = sitzplatzSchluessel(sitzplatz.reihe, sitzplatz.spalte);
    const neueAuswahl = new Map(this.auswahl());

    if (neueAuswahl.has(schluessel)) {
      neueAuswahl.delete(schluessel);
    } else {
      const ersteKategorie = this.sitzplan()?.preiskategorien[0];
      if (!ersteKategorie) {
        return;
      }
      neueAuswahl.set(schluessel, {
        reihe: sitzplatz.reihe,
        spalte: sitzplatz.spalte,
        preiskategorieId: ersteKategorie.id
      });
    }

    this.auswahl.set(neueAuswahl);
  }

  kategorieAendern(sitzplatz: AusgewaehlterSitzplatz, preiskategorieId: string): void {
    const schluessel = sitzplatzSchluessel(sitzplatz.reihe, sitzplatz.spalte);
    const neueAuswahl = new Map(this.auswahl());
    neueAuswahl.set(schluessel, { ...sitzplatz, preiskategorieId });
    this.auswahl.set(neueAuswahl);
  }

  entfernen(sitzplatz: AusgewaehlterSitzplatz): void {
    const schluessel = sitzplatzSchluessel(sitzplatz.reihe, sitzplatz.spalte);
    const neueAuswahl = new Map(this.auswahl());
    neueAuswahl.delete(schluessel);
    this.auswahl.set(neueAuswahl);
  }

  jetztBuchen(): void {
    if (this.ausgewaehlteListe().length === 0 || !this.name.trim() || !this.email.trim()) {
      return;
    }

    this.wirdGebucht.set(true);
    this.buchungsFehler.set(null);

    this.buchungenService
      .create({
        veranstaltungId: this.veranstaltungId,
        name: this.name.trim(),
        email: this.email.trim(),
        sitzplaetze: this.ausgewaehlteListe().map(({ reihe, spalte, preiskategorieId }) => ({ reihe, spalte, preiskategorieId }))
      })
      .subscribe({
        next: (buchung) => {
          this.wirdGebucht.set(false);
          this.router.navigate(['/buchungen', buchung.referenz]);
        },
        error: (fehler: HttpErrorResponse) => {
          this.wirdGebucht.set(false);
          const problem = fehler.error as ApiProblem | undefined;

          if (fehler.status === 409 && problem?.belegteSitzplaetze) {
            const belegteSchluessel = new Set(
              problem.belegteSitzplaetze.map((s) => sitzplatzSchluessel(s.reihe, s.spalte))
            );
            const neueAuswahl = new Map(this.auswahl());
            for (const schluessel of belegteSchluessel) {
              neueAuswahl.delete(schluessel);
            }
            this.auswahl.set(neueAuswahl);
            this.buchungsFehler.set(
              'Mindestens ein ausgewählter Sitzplatz wurde inzwischen von einer anderen Person belegt. Die betroffenen Plätze wurden aus Ihrer Auswahl entfernt.'
            );
            this.ladeDaten();
            return;
          }

          this.buchungsFehler.set(problem?.detail ?? 'Die Buchung konnte nicht abgeschlossen werden.');
        }
      });
  }
}
