export interface BuchungAnlegenRequest {
  name: string;
  email: string;
  positionen: BuchungspositionRequest[];
}

export interface BuchungspositionRequest {
  sitzplatzCode: string;
  preiskategorieId: string;
}

export interface Buchung {
  referenz: string;
  veranstaltungId: string;
  name: string;
  email: string;
  status: 'Aktiv' | 'Storniert';
  gesamtpreis: number;
  positionen: Buchungsposition[];
}

export interface Buchungsposition {
  sitzplatzCode: string;
  preiskategorieId: string;
  preiskategorieName: string;
  preisSnapshot: number;
}
