export interface VeranstaltungListItem {
  id: string;
  titel: string;
  spielstaetteId: string;
  spielstaetteName: string;
  zeitpunkt: string;
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
  dauerMinuten: number;
  altersfreigabe: number;
  spielstaetteId: string;
  spielstaetteName: string;
  raumId: string;
  raumName: string;
  zeitpunkt: string;
  preiskategorien: Preiskategorie[];
}

export interface SitzplatzPosition {
  reihe: string;
  spalte: number;
}

export interface Sitzplan {
  raumName: string;
  reihen: string[];
  spalten: number;
  gangSpalten: number[];
  gangHinweis: string | null;
  belegtePlaetze: SitzplatzPosition[];
}
