export type SeatStatus = 'Free' | 'Occupied';

export interface Seat {
  seatId: string;
  row: string;
  column: number;
  status: SeatStatus;
}

export interface PriceCategory {
  id: string;
  name: string;
  price: number;
}

export interface SeatMap {
  eventId: string;
  rows: string[];
  columns: number;
  aisleColumns: number[];
  seats: Seat[];
  priceCategories: PriceCategory[];
}
