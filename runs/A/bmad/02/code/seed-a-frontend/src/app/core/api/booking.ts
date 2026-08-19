import { PriceCategory } from './event-detail';

export type BookingStatus = 'aktiv' | 'storniert';

export interface BookingPosition {
  rowLabel: string;
  columnNumber: number;
  preiskategorie: PriceCategory;
}

export interface Booking {
  reference: string;
  name: string;
  status: BookingStatus;
  positionen: BookingPosition[];
  gesamtpreis: number;
}

export interface CreateBookingPositionRequest {
  rowLabel: string;
  columnNumber: number;
  priceCategoryId: number;
}

export interface CreateBookingRequest {
  eventId: number;
  name: string;
  email: string;
  positionen: CreateBookingPositionRequest[];
}

export function parseSeatCode(seatCode: string): { rowLabel: string; columnNumber: number } {
  const match = /^([A-Za-z]+)(\d+)$/.exec(seatCode);
  if (!match) {
    throw new Error(`Ungültiger Sitzplatz-Code: '${seatCode}'`);
  }
  return { rowLabel: match[1], columnNumber: Number(match[2]) };
}
