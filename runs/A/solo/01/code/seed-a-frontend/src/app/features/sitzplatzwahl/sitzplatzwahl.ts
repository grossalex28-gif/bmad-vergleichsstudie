import { DecimalPipe } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import { fehlermeldung } from '../../core/api-fehler';
import { Sitzplan, SitzplatzPosition, VeranstaltungDetail } from '../../core/models/veranstaltung.model';
import { BuchungenService } from '../../core/services/buchungen.service';
import { VeranstaltungenService } from '../../core/services/veranstaltungen.service';
import { SitzplanComponent } from '../../shared/sitzplan/sitzplan';

interface AusgewaehlterSitzplatz extends SitzplatzPosition {
  preiskategorieId: string;
}

@Component({
  selector: 'app-sitzplatzwahl',
  imports: [FormsModule, RouterLink, SitzplanComponent, DecimalPipe],
  templateUrl: './sitzplatzwahl.html',
  styleUrl: './sitzplatzwahl.scss'
})
export class SitzplatzwahlComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly veranstaltungenService = inject(VeranstaltungenService);
  private readonly buchungenService = inject(BuchungenService);

  protected readonly veranstaltungId = this.route.snapshot.paramMap.get('id')!;
  protected readonly veranstaltung = signal<VeranstaltungDetail | null>(null);
  protected readonly sitzplan = signal<Sitzplan | null>(null);
  protected readonly ausgewaehlteSitzplaetze = signal<AusgewaehlterSitzplatz[]>([]);
  protected readonly ladeFehler = signal<string | null>(null);
  protected readonly buchungFehler = signal<string | null>(null);
  protected readonly wirdGebucht = signal(false);

  protected readonly ausgewaehltePositionen = computed<SitzplatzPosition[]>(() =>
    this.ausgewaehlteSitzplaetze().map(({ reihe, spalte }) => ({ reihe, spalte }))
  );

  protected readonly gesamtpreis = computed(() => {
    const veranstaltung = this.veranstaltung();
    if (!veranstaltung) {
      return 0;
    }
    return this.ausgewaehlteSitzplaetze().reduce((summe, sitzplatz) => {
      const kategorie = veranstaltung.preiskategorien.find((k) => k.id === sitzplatz.preiskategorieId);
      return summe + (kategorie?.preis ?? 0);
    }, 0);
  });

  protected name = '';
  protected email = '';

  constructor() {
    forkJoin({
      veranstaltung: this.veranstaltungenService.getEine(this.veranstaltungId),
      sitzplan: this.veranstaltungenService.getSitzplan(this.veranstaltungId)
    }).subscribe({
      next: ({ veranstaltung, sitzplan }) => {
        this.veranstaltung.set(veranstaltung);
        this.sitzplan.set(sitzplan);
      },
      error: (fehler) =>
        this.ladeFehler.set(fehlermeldung(fehler, 'Der Sitzplan konnte nicht geladen werden.'))
    });
  }

  protected sitzplatzUmschalten(position: SitzplatzPosition): void {
    const aktuelle = this.ausgewaehlteSitzplaetze();
    const istAusgewaehlt = aktuelle.some((p) => p.reihe === position.reihe && p.spalte === position.spalte);

    if (istAusgewaehlt) {
      this.ausgewaehlteSitzplaetze.set(
        aktuelle.filter((p) => !(p.reihe === position.reihe && p.spalte === position.spalte))
      );
      return;
    }

    const ersteKategorieId = this.veranstaltung()?.preiskategorien[0]?.id ?? '';
    this.ausgewaehlteSitzplaetze.set([...aktuelle, { ...position, preiskategorieId: ersteKategorieId }]);
  }

  protected preisFuer(preiskategorieId: string): number {
    return this.veranstaltung()?.preiskategorien.find((k) => k.id === preiskategorieId)?.preis ?? 0;
  }

  protected preiskategorieAendern(position: SitzplatzPosition, preiskategorieId: string): void {
    this.ausgewaehlteSitzplaetze.set(
      this.ausgewaehlteSitzplaetze().map((p) =>
        p.reihe === position.reihe && p.spalte === position.spalte ? { ...p, preiskategorieId } : p
      )
    );
  }

  protected buchen(): void {
    if (this.ausgewaehlteSitzplaetze().length === 0 || !this.name.trim() || !this.email.trim()) {
      return;
    }

    this.wirdGebucht.set(true);
    this.buchungFehler.set(null);

    this.buchungenService
      .erstellen({
        veranstaltungId: this.veranstaltungId,
        name: this.name.trim(),
        email: this.email.trim(),
        sitzplaetze: this.ausgewaehlteSitzplaetze().map(({ reihe, spalte, preiskategorieId }) => ({
          reihe,
          spalte,
          preiskategorieId
        }))
      })
      .subscribe({
        next: (buchung) => this.router.navigate(['/buchungen', buchung.referenz]),
        error: (fehler) => {
          this.wirdGebucht.set(false);
          this.buchungFehler.set(
            fehlermeldung(fehler, 'Die Buchung konnte nicht abgeschlossen werden.')
          );
          // Bei einem Sitzplatzkonflikt (A-F13) den Sitzplan neu laden, damit inzwischen
          // belegte Plätze korrekt angezeigt werden, und die Auswahl zurücksetzen.
          this.ausgewaehlteSitzplaetze.set([]);
          this.veranstaltungenService.getSitzplan(this.veranstaltungId).subscribe({
            next: (sitzplan) => this.sitzplan.set(sitzplan)
          });
        }
      });
  }
}
