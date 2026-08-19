export interface Lieferdaten {
  name: string;
  strasse: string;
  plz: string;
  ort: string;
  land: string;
}

export interface Kontaktdaten {
  email: string;
  telefon: string;
}

export interface PositionCreate {
  produktId: string;
  lieferantId: string;
  menge: number;
}

export interface OrderCreate {
  lieferdaten: Lieferdaten;
  kontakt: Kontaktdaten;
  positionen: PositionCreate[];
}

export interface OrderItemResponse {
  produktId: string;
  produktName: string;
  lieferantId: string;
  lieferantName: string;
  preis: number;
  menge: number;
  zwischensumme: number;
}

export interface OrderResponse {
  id: number;
  status: string;
  erstelltAm: string;
  lieferdaten: Lieferdaten;
  kontakt: Kontaktdaten;
  positionen: OrderItemResponse[];
  gesamtsumme: number;
}
