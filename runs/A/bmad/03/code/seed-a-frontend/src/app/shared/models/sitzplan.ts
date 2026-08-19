export interface Sitzplan {
  veranstaltungId: string;
  raumId: string;
  raumName: string;
  reihen: SitzplanReihe[];
}

export interface SitzplanReihe {
  reihe: string;
  positionen: SitzplanPosition[];
}

export interface SitzplanPosition {
  spalte: number;
  typ: 'Sitzplatz' | 'Gang';
  code: string | null;
  status: 'Frei' | 'Belegt' | null;
}
