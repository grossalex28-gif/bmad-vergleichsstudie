export interface Spielstaette {
  id: string;
  name: string;
}

export interface VeranstaltungListItem {
  id: string;
  titel: string;
  spielstaetteId: string;
  spielstaetteName: string;
  zeitpunkt: string;
}

export interface RaumInfo {
  id: string;
  name: string;
}

export interface Preiskategorie {
  id: string;
  name: string;
  preis: number;
}

export interface VeranstaltungDetail {
  id: string;
  titel: string;
  beschreibung: string;
  zeitpunkt: string;
  dauerMinuten: number;
  altersfreigabe: number;
  spielstaette: Spielstaette;
  raum: RaumInfo;
  preiskategorien: Preiskategorie[];
}

export type SitzplatzTyp = 'Sitzplatz' | 'Gang';
export type SitzplatzStatus = 'Frei' | 'Belegt';

export interface Sitzplatz {
  reihe: string;
  spalte: number;
  typ: SitzplatzTyp;
  status: SitzplatzStatus | null;
}

export interface RaumSitzplan {
  reihen: string[];
  spalten: number;
  gangSpalten: number[];
  gangHinweis: string | null;
}

export interface Sitzplan {
  veranstaltungId: string;
  raum: RaumSitzplan;
  sitzplaetze: Sitzplatz[];
  preiskategorien: Preiskategorie[];
}

export interface SitzplatzAuswahl {
  reihe: string;
  spalte: number;
  preiskategorieId: string;
}

export interface CreateBuchungRequest {
  veranstaltungId: string;
  name: string;
  email: string;
  sitzplaetze: SitzplatzAuswahl[];
}

export interface Buchungsposition {
  reihe: string;
  spalte: number;
  preiskategorieId: string;
  preiskategorieName: string;
  preis: number;
}

export type BuchungStatus = 'Bestaetigt' | 'Storniert';

export interface Buchung {
  referenz: string;
  veranstaltungId: string;
  veranstaltungTitel: string;
  veranstaltungZeitpunkt: string;
  name: string;
  email: string;
  erstelltAm: string;
  status: BuchungStatus;
  sitzplaetze: Buchungsposition[];
  gesamtpreis: number;
}

export interface BesetzterSitzplatz {
  reihe: string;
  spalte: number;
}

export interface ApiProblem {
  title?: string;
  status?: number;
  detail?: string;
  belegteSitzplaetze?: BesetzterSitzplatz[];
}
