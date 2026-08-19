import { Component, DestroyRef, ElementRef, computed, inject, signal, viewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CurrencyPipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { ActivatedRoute } from '@angular/router';
import { EMPTY, catchError, forkJoin, map, switchMap } from 'rxjs';
import { BuchungenService } from '../shared/api/buchungen.service';
import { VeranstaltungenService } from '../shared/api/veranstaltungen.service';
import { Buchung as BuchungDto, BuchungspositionRequest } from '../shared/models/buchung';
import { Preiskategorie } from '../shared/models/preiskategorie';
import { ProblemDetails } from '../shared/models/problem-details';
import { Sitzplan, SitzplanPosition } from '../shared/models/sitzplan';
import { Buchungsreferenz } from '../shared/ui/buchungsreferenz/buchungsreferenz';
import { SeatTile } from '../shared/ui/seat-tile/seat-tile';

type Ladezustand = 'laedt' | 'geladen' | 'fehler';
type Position = { reihe: string; spalte: number };

@Component({
  selector: 'app-buchung',
  imports: [SeatTile, CurrencyPipe, Buchungsreferenz],
  templateUrl: './buchung.html',
  styleUrl: './buchung.scss',
})
export class Buchung {
  private readonly veranstaltungenService = inject(VeranstaltungenService);
  private readonly buchungenService = inject(BuchungenService);
  private readonly route = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);

  readonly ladezustand = signal<Ladezustand>('laedt');
  readonly sitzplan = signal<Sitzplan | null>(null);
  readonly preiskategorien = signal<Preiskategorie[]>([]);
  readonly ausgewaehlteCodes = signal<ReadonlySet<string>>(new Set());
  readonly kategorieZuordnung = signal<ReadonlyMap<string, string>>(new Map());
  readonly fokussiertePosition = signal<Position | null>(null);
  readonly detailsAusgeklappt = signal(false);
  readonly schritt = signal<1 | 2>(1);
  readonly kontaktName = signal('');
  readonly kontaktEmail = signal('');
  readonly emailBeruehrt = signal(false);
  readonly abschliessenLaedt = signal(false);
  readonly abschliessenFehler = signal<string | null>(null);
  readonly konfliktSitzplaetze = signal<string[]>([]);
  readonly abgeschlosseneBuchung = signal<BuchungDto | null>(null);

  private static readonly EMAIL_MUSTER = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

  private readonly sitzplanRef = viewChild<ElementRef<HTMLElement>>('sitzplanContainer');

  protected readonly skeletonReihen = [0, 1, 2, 3, 4, 5];
  protected readonly skeletonSpalten = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9];

  protected readonly ausgewaehlteCodesListe = computed(() => Array.from(this.ausgewaehlteCodes()));

  protected readonly weiterDeaktiviert = computed(() => {
    const codes = this.ausgewaehlteCodes();
    if (codes.size === 0) return true;
    const zuordnung = this.kategorieZuordnung();
    return [...codes].some((code) => !zuordnung.has(code));
  });

  readonly gesamtpreis = computed(() => {
    const zuordnung = this.kategorieZuordnung();
    const kategorien = this.preiskategorien();
    let summe = 0;
    for (const preiskategorieId of zuordnung.values()) {
      const kategorie = kategorien.find((k) => k.id === preiskategorieId);
      if (kategorie) summe += kategorie.preis;
    }
    return summe;
  });

  protected readonly anzahlAusgewaehlterPlaetze = computed(() => this.ausgewaehlteCodes().size);

  protected readonly emailGueltig = computed(() =>
    Buchung.EMAIL_MUSTER.test(this.kontaktEmail().trim()),
  );
  protected readonly zeigeEmailFehler = computed(
    () => this.emailBeruehrt() && this.kontaktEmail().trim().length > 0 && !this.emailGueltig(),
  );
  protected readonly buchungAbschliessenDeaktiviert = computed(
    () => this.kontaktName().trim().length === 0 || !this.emailGueltig(),
  );
  protected readonly konfliktNachricht = computed(() => {
    const codes = this.konfliktSitzplaetze();
    if (codes.length === 0) return '';
    if (codes.length === 1) return `Sitzplatz ${codes[0]} ist inzwischen belegt.`;
    return `Sitzplätze ${codes.join(', ')} sind inzwischen belegt.`;
  });

  constructor() {
    this.route.paramMap
      .pipe(
        map((paramMap) => paramMap.get('id')!),
        switchMap((id) => {
          this.ladezustand.set('laedt');
          return forkJoin({
            sitzplan: this.veranstaltungenService.getSitzplan(id),
            veranstaltung: this.veranstaltungenService.getVeranstaltung(id),
          }).pipe(
            catchError(() => {
              this.ladezustand.set('fehler');
              return EMPTY;
            }),
          );
        }),
        takeUntilDestroyed(),
      )
      .subscribe(({ sitzplan, veranstaltung }) => {
        this.sitzplan.set(sitzplan);
        this.preiskategorien.set(veranstaltung.preiskategorien);
        this.ladezustand.set('geladen');
        this.ausgewaehlteCodes.set(new Set());
        this.kategorieZuordnung.set(new Map());
        this.fokussiertePosition.set(this.ersteSitzplatzPosition(sitzplan));
        this.detailsAusgeklappt.set(false);
        this.schritt.set(1);
        this.kontaktName.set('');
        this.kontaktEmail.set('');
        this.emailBeruehrt.set(false);
        this.abschliessenLaedt.set(false);
        this.abschliessenFehler.set(null);
        this.konfliktSitzplaetze.set([]);
        this.abgeschlosseneBuchung.set(null);
      });
  }

  protected detailsUmschalten(): void {
    this.detailsAusgeklappt.update((offen) => !offen);
  }

  protected weiterZuKontakt(): void {
    this.abschliessenFehler.set(null);
    this.schritt.set(2);
  }

  protected zurueckZuSitzplan(): void {
    this.schritt.set(1);
  }

  protected buchungAbschliessen(): void {
    if (this.buchungAbschliessenDeaktiviert() || this.abschliessenLaedt()) return;
    const plan = this.sitzplan();
    if (!plan) return;

    this.abschliessenLaedt.set(true);
    this.abschliessenFehler.set(null);
    this.konfliktSitzplaetze.set([]);

    const positionen: BuchungspositionRequest[] = this.ausgewaehlteCodesListe().map((code) => ({
      sitzplatzCode: code,
      preiskategorieId: this.kategorieZuordnung().get(code)!,
    }));

    this.buchungenService
      .buchungAnlegen(plan.veranstaltungId, {
        name: this.kontaktName().trim(),
        email: this.kontaktEmail().trim(),
        positionen,
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (buchung) => {
          this.abschliessenLaedt.set(false);
          this.abgeschlosseneBuchung.set(buchung);
        },
        error: (fehler: HttpErrorResponse) => {
          this.abschliessenLaedt.set(false);
          this.aufBuchungsfehlerReagieren(fehler);
        },
      });
  }

  private aufBuchungsfehlerReagieren(fehler: HttpErrorResponse): void {
    const problem = fehler.error as ProblemDetails | null;
    const betroffen = problem?.betroffeneSitzplaetze;

    if (fehler.status === 409 && problem?.type === 'sitzplatz_belegt' && betroffen?.length) {
      this.konfliktSitzplaetze.set(betroffen);
      this.schritt.set(1);

      const naechsteCodes = new Set(this.ausgewaehlteCodes());
      const naechsteZuordnung = new Map(this.kategorieZuordnung());
      for (const code of betroffen) {
        naechsteCodes.delete(code);
        naechsteZuordnung.delete(code);
      }
      this.ausgewaehlteCodes.set(naechsteCodes);
      this.kategorieZuordnung.set(naechsteZuordnung);
      if (naechsteCodes.size === 0) {
        this.detailsAusgeklappt.set(false);
      }

      const veranstaltungId = this.sitzplan()?.veranstaltungId;
      if (veranstaltungId) {
        this.veranstaltungenService
          .getSitzplan(veranstaltungId)
          .pipe(takeUntilDestroyed(this.destroyRef))
          .subscribe({
            next: (neuerPlan) => this.sitzplan.set(neuerPlan),
            error: () => {},
          });
      }
      return;
    }

    this.abschliessenFehler.set(
      'Die Buchung konnte nicht abgeschlossen werden. Bitte versuchen Sie es erneut.',
    );
  }

  protected kategorieZuweisen(code: string, preiskategorieId: string): void {
    const naechste = new Map(this.kategorieZuordnung());
    naechste.set(code, preiskategorieId);
    this.kategorieZuordnung.set(naechste);
  }

  protected auswahlUmschalten(code: string): void {
    const naechsteCodes = new Set(this.ausgewaehlteCodes());
    if (naechsteCodes.has(code)) {
      naechsteCodes.delete(code);
      const naechsteZuordnung = new Map(this.kategorieZuordnung());
      naechsteZuordnung.delete(code);
      this.kategorieZuordnung.set(naechsteZuordnung);
      if (naechsteCodes.size === 0) {
        this.detailsAusgeklappt.set(false);
      }
    } else {
      naechsteCodes.add(code);
    }
    this.ausgewaehlteCodes.set(naechsteCodes);
  }

  protected aufKachelKlick(reihe: string, spalte: number, code: string): void {
    this.fokussiertePosition.set({ reihe, spalte });
    this.auswahlUmschalten(code);
  }

  protected istFokussiert(reihe: string, spalte: number): boolean {
    const f = this.fokussiertePosition();
    return f !== null && f.reihe === reihe && f.spalte === spalte;
  }

  protected onSitzplanKeydown(event: KeyboardEvent): void {
    const plan = this.sitzplan();
    const aktuell = this.fokussiertePosition();
    if (!plan || !aktuell) return;

    switch (event.key) {
      case 'ArrowRight':
        event.preventDefault();
        this.bewegeHorizontal(plan, aktuell, 1);
        break;
      case 'ArrowLeft':
        event.preventDefault();
        this.bewegeHorizontal(plan, aktuell, -1);
        break;
      case 'ArrowDown':
        event.preventDefault();
        this.bewegeVertikal(plan, aktuell, 1);
        break;
      case 'ArrowUp':
        event.preventDefault();
        this.bewegeVertikal(plan, aktuell, -1);
        break;
      case ' ':
      case 'Enter':
        event.preventDefault();
        this.aktiviereFokussiertePosition(plan, aktuell);
        break;
    }
  }

  private aktiviereFokussiertePosition(plan: Sitzplan, aktuell: Position): void {
    const reihe = plan.reihen.find((r) => r.reihe === aktuell.reihe);
    const position = reihe?.positionen.find((p) => p.spalte === aktuell.spalte);
    if (!position || position.status === 'Belegt' || !position.code) return;
    this.auswahlUmschalten(position.code);
  }

  private bewegeHorizontal(plan: Sitzplan, aktuell: Position, richtung: 1 | -1): void {
    const reihe = plan.reihen.find((r) => r.reihe === aktuell.reihe);
    if (!reihe) return;
    let spalte = aktuell.spalte + richtung;
    while (spalte >= 1 && spalte <= reihe.positionen.length) {
      const position = reihe.positionen.find((p) => p.spalte === spalte);
      if (position?.typ === 'Sitzplatz') {
        this.setzeFokus({ reihe: aktuell.reihe, spalte });
        return;
      }
      spalte += richtung;
    }
  }

  private bewegeVertikal(plan: Sitzplan, aktuell: Position, richtung: 1 | -1): void {
    const rowIndex = plan.reihen.findIndex((r) => r.reihe === aktuell.reihe);
    const naechsterIndex = rowIndex + richtung;
    if (naechsterIndex < 0 || naechsterIndex >= plan.reihen.length) return;
    const naechsteReihe = plan.reihen[naechsterIndex];
    const position = naechsteReihe.positionen.find((p) => p.spalte === aktuell.spalte);
    if (!position) return;
    this.setzeFokus({ reihe: naechsteReihe.reihe, spalte: aktuell.spalte });
  }

  private setzeFokus(position: Position): void {
    this.fokussiertePosition.set(position);
    const code = `${position.reihe}${position.spalte}`;
    (
      this.sitzplanRef()?.nativeElement.querySelector(`[data-code="${code}"]`) as HTMLElement | null
    )?.focus();
  }

  private ersteSitzplatzPosition(plan: Sitzplan): Position | null {
    for (const reihe of plan.reihen) {
      const position = reihe.positionen.find((p: SitzplanPosition) => p.typ === 'Sitzplatz');
      if (position) return { reihe: reihe.reihe, spalte: position.spalte };
    }
    return null;
  }
}
