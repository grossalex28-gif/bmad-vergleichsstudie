import { CurrencyPipe, DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, ElementRef, inject, signal, viewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute } from '@angular/router';
import { EMPTY, catchError, map, switchMap } from 'rxjs';
import { BuchungenService } from '../shared/api/buchungen.service';
import { BuchungDetail } from '../shared/models/buchung-detail';
import { BadgeStatus } from '../shared/ui/badge-status/badge-status';
import { Buchungsreferenz } from '../shared/ui/buchungsreferenz/buchungsreferenz';
import { spielstaettenOffset } from '../shared/utils/zeitzone';

type Ladezustand = 'laedt' | 'geladen' | 'fehler';

@Component({
  selector: 'app-buchungsdetail',
  imports: [CurrencyPipe, DatePipe, BadgeStatus, Buchungsreferenz],
  templateUrl: './buchungsdetail.html',
  styleUrl: './buchungsdetail.scss',
})
export class Buchungsdetail {
  private readonly buchungenService = inject(BuchungenService);
  private readonly route = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);

  readonly ladezustand = signal<Ladezustand>('laedt');
  readonly buchung = signal<BuchungDetail | null>(null);
  readonly stornierungLaeuft = signal(false);
  readonly stornierungErfolgreich = signal(false);
  readonly stornierungFehler = signal<string | null>(null);

  protected readonly spielstaettenOffset = spielstaettenOffset;
  private readonly stornierenDialog =
    viewChild.required<ElementRef<HTMLDialogElement>>('stornierenDialog');

  constructor() {
    this.route.paramMap
      .pipe(
        map((paramMap) => paramMap.get('referenz')!),
        switchMap((referenz) => {
          this.ladezustand.set('laedt');
          this.stornierungErfolgreich.set(false);
          this.stornierungFehler.set(null);
          return this.buchungenService.buchungAbrufen(referenz).pipe(
            catchError(() => {
              this.ladezustand.set('fehler');
              return EMPTY;
            }),
          );
        }),
        takeUntilDestroyed(),
      )
      .subscribe((buchung) => {
        this.buchung.set(buchung);
        this.ladezustand.set('geladen');
      });
  }

  protected dialogOeffnen(): void {
    this.stornierungFehler.set(null);
    this.stornierenDialog().nativeElement.showModal();
  }

  protected dialogAbbrechen(): void {
    this.stornierenDialog().nativeElement.close();
  }

  protected stornierenBestaetigen(): void {
    const aktuelleBuchung = this.buchung();
    if (!aktuelleBuchung || this.stornierungLaeuft()) return;

    this.stornierungLaeuft.set(true);
    this.buchungenService
      .buchungStornieren(aktuelleBuchung.referenz)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (aktualisiert) => {
          this.stornierungLaeuft.set(false);
          this.buchung.set(aktualisiert);
          this.stornierungErfolgreich.set(true);
          this.stornierenDialog().nativeElement.close();
        },
        error: (fehler: HttpErrorResponse) => {
          this.stornierungLaeuft.set(false);
          if (fehler.status === 409) {
            const aktuelleBuchung = this.buchung();
            if (aktuelleBuchung) {
              this.buchung.set({ ...aktuelleBuchung, status: 'Storniert' });
            }
            this.stornierungFehler.set('Diese Buchung wurde bereits storniert.');
          } else {
            this.stornierungFehler.set(
              'Stornierung fehlgeschlagen. Bitte versuchen Sie es erneut.',
            );
          }
          this.stornierenDialog().nativeElement.close();
        },
      });
  }
}
