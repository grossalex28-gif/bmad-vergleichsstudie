export interface BuchungPositionRequest {
  reihe: string;
  spalte: number;
  preiskategorieId: string;
}

export interface BuchungCreateRequest {
  veranstaltungId: string;
  name: string;
  email: string;
  sitzplaetze: BuchungPositionRequest[];
}

export interface BuchungPositionResponse {
  reihe: string;
  spalte: number;
  preiskategorieId: string;
  preiskategorieName: string;
  preis: number;
}

export interface Buchung {
  referenz: string;
  name: string;
  email: string;
  status: 'Aktiv' | 'Storniert';
  erstelltAm: string;
  veranstaltungId: string;
  veranstaltungTitel: string;
  veranstaltungZeitpunkt: string;
  spielstaetteName: string;
  raumName: string;
  positionen: BuchungPositionResponse[];
  gesamtpreis: number;
}
