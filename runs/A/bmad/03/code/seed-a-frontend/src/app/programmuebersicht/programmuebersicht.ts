import { Component, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { EMPTY, catchError, switchMap } from 'rxjs';
import { VeranstaltungenService } from '../shared/api/veranstaltungen.service';
import { Veranstaltung } from '../shared/models/veranstaltung';
import { EventCard } from '../shared/ui/event-card/event-card';
import { FilterBar } from '../shared/ui/filter-bar/filter-bar';

type Ladezustand = 'laedt' | 'geladen' | 'fehler';

@Component({
  selector: 'app-programmuebersicht',
  imports: [EventCard, FilterBar],
  templateUrl: './programmuebersicht.html',
  styleUrl: './programmuebersicht.scss'
})
export class Programmuebersicht {
  private readonly veranstaltungenService = inject(VeranstaltungenService);

  readonly von = signal<string | null>(null);
  readonly bis = signal<string | null>(null);
  readonly spielstaetteId = signal<string | null>(null);

  readonly ladezustand = signal<Ladezustand>('laedt');
  readonly veranstaltungen = signal<Veranstaltung[]>([]);
  readonly spielstaetten = signal<{ id: string; name: string }[]>([]);

  protected readonly skeletonPlatzhalter = [0, 1, 2, 3];

  protected readonly filterAktiv = computed(
    () => this.von() !== null || this.bis() !== null || this.spielstaetteId() !== null
  );

  private readonly filter = computed(() => ({
    von: this.von(),
    bis: this.bis(),
    spielstaetteId: this.spielstaetteId()
  }));

  protected filterZuruecksetzen(): void {
    this.von.set(null);
    this.bis.set(null);
    this.spielstaetteId.set(null);
  }

  constructor() {
    toObservable(this.filter)
      .pipe(
        switchMap((filter) => {
          this.ladezustand.set('laedt');
          return this.veranstaltungenService
            .getVeranstaltungen(filter.von, filter.bis, filter.spielstaetteId)
            .pipe(
              catchError(() => {
                this.ladezustand.set('fehler');
                return EMPTY;
              })
            );
        }),
        takeUntilDestroyed()
      )
      .subscribe((veranstaltungen) => {
        this.veranstaltungen.set(veranstaltungen);
        this.ladezustand.set('geladen');
      });

    this.veranstaltungenService
      .getVeranstaltungen()
      .pipe(
        catchError((fehler: unknown) => {
          console.error('Spielstätten für Filter-Dropdown konnten nicht geladen werden', fehler);
          return EMPTY;
        }),
        takeUntilDestroyed()
      )
      .subscribe((veranstaltungen) => {
        const eindeutigeSpielstaetten = new Map<string, string>();
        for (const veranstaltung of veranstaltungen) {
          eindeutigeSpielstaetten.set(veranstaltung.spielstaetteId, veranstaltung.spielstaetteName);
        }
        this.spielstaetten.set(
          [...eindeutigeSpielstaetten.entries()]
            .map(([id, name]) => ({ id, name }))
            .sort((a, b) => a.name.localeCompare(b.name, 'de'))
        );
      });
  }
}
