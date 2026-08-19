import { Component, computed, input, output } from '@angular/core';
import { Sitzplan, SitzplatzPosition } from '../../core/models/veranstaltung.model';

type ZellTyp = 'sitz' | 'gang';
type SitzStatus = 'frei' | 'belegt' | 'ausgewaehlt';

interface Zelle {
  spalte: number;
  typ: ZellTyp;
  status: SitzStatus;
}

interface Zeile {
  reihe: string;
  zellen: Zelle[];
}

function schluessel(position: SitzplatzPosition): string {
  return `${position.reihe}-${position.spalte}`;
}

@Component({
  selector: 'app-sitzplan',
  templateUrl: './sitzplan.html',
  styleUrl: './sitzplan.scss'
})
export class SitzplanComponent {
  readonly sitzplan = input.required<Sitzplan>();
  readonly ausgewaehlt = input<SitzplatzPosition[]>([]);
  readonly sitzplatzUmschalten = output<SitzplatzPosition>();

  protected readonly zeilen = computed<Zeile[]>(() => {
    const plan = this.sitzplan();
    const gangSpalten = new Set(plan.gangSpalten);
    const belegt = new Set(plan.belegtePlaetze.map(schluessel));
    const ausgewaehlteSchluessel = new Set(this.ausgewaehlt().map(schluessel));

    return plan.reihen.map((reihe) => ({
      reihe,
      zellen: Array.from({ length: plan.spalten }, (_, index) => {
        const spalte = index + 1;
        if (gangSpalten.has(spalte)) {
          return { spalte, typ: 'gang' as const, status: 'frei' as const };
        }

        const key = `${reihe}-${spalte}`;
        const status: SitzStatus = belegt.has(key)
          ? 'belegt'
          : ausgewaehlteSchluessel.has(key)
            ? 'ausgewaehlt'
            : 'frei';

        return { spalte, typ: 'sitz' as const, status };
      })
    }));
  });

  protected sitzplatzKlick(reihe: string, zelle: Zelle): void {
    if (zelle.typ !== 'sitz' || zelle.status === 'belegt') {
      return;
    }

    this.sitzplatzUmschalten.emit({ reihe, spalte: zelle.spalte });
  }

  protected sitzplatzLabel(reihe: string, zelle: Zelle): string {
    if (zelle.typ === 'gang') {
      return '';
    }
    return `Sitzplatz ${reihe}${zelle.spalte}, ${zelle.status}`;
  }
}
