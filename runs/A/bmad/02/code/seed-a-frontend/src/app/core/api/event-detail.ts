export interface PriceCategory {
  id: number;
  name: string;
  preis: number;
}

export interface EventDetail {
  id: number;
  titel: string;
  beschreibung: string;
  dauerMinuten: number;
  altersfreigabe: number;
  spielstaette: string;
  raum: string;
  zeitpunkt: string;
  preiskategorien: PriceCategory[];
}
