import { DatePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { EMPTY, catchError, map, switchMap } from 'rxjs';
import { VeranstaltungenService } from '../shared/api/veranstaltungen.service';
import { VeranstaltungDetail } from '../shared/models/veranstaltung-detail';
import { spielstaettenOffset } from '../shared/utils/zeitzone';

type Ladezustand = 'laedt' | 'geladen' | 'fehler';

@Component({
  selector: 'app-veranstaltungsdetail',
  imports: [DatePipe, RouterLink],
  templateUrl: './veranstaltungsdetail.html',
  styleUrl: './veranstaltungsdetail.scss'
})
export class Veranstaltungsdetail {
  private readonly veranstaltungenService = inject(VeranstaltungenService);
  private readonly route = inject(ActivatedRoute);

  readonly ladezustand = signal<Ladezustand>('laedt');
  readonly veranstaltung = signal<VeranstaltungDetail | null>(null);

  protected readonly spielstaettenOffset = spielstaettenOffset;

  constructor() {
    this.route.paramMap
      .pipe(
        map((paramMap) => paramMap.get('id')!),
        switchMap((id) => {
          this.ladezustand.set('laedt');
          return this.veranstaltungenService.getVeranstaltung(id).pipe(
            catchError(() => {
              this.ladezustand.set('fehler');
              return EMPTY;
            })
          );
        }),
        takeUntilDestroyed()
      )
      .subscribe((veranstaltung) => {
        this.veranstaltung.set(veranstaltung);
        this.ladezustand.set('geladen');
      });
  }
}
