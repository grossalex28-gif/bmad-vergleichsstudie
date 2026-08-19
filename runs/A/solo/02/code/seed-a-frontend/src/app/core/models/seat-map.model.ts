export type SeatType = 'seat' | 'aisle';
export type SeatStatus = 'free' | 'occupied' | 'aisle';

export interface Seat {
  column: number;
  type: SeatType;
  status: SeatStatus;
}

export interface SeatRow {
  row: string;
  seats: Seat[];
}

export interface SeatMap {
  eventId: string;
  roomId: string;
  roomName: string;
  rowLabels: string[];
  columns: number;
  rows: SeatRow[];
}
