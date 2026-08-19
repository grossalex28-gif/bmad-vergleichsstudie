import { Buchungsposition } from './buchung';

export interface BuchungDetail {
  referenz: string;
  veranstaltungId: string;
  veranstaltungTitel: string;
  veranstaltungZeitpunkt: string;
  spielstaetteName: string;
  raumName: string;
  name: string;
  email: string;
  status: 'Aktiv' | 'Storniert';
  gesamtpreis: number;
  positionen: Buchungsposition[];
}
