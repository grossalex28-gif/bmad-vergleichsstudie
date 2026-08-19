export type SeatStatus = 'free' | 'occupied' | 'aisle';

export interface SeatMapCell {
  columnNumber: number;
  status: SeatStatus;
}

export interface SeatMapRow {
  rowLabel: string;
  cells: SeatMapCell[];
}

export interface SeatMap {
  rowLabels: string[];
  columnCount: number;
  aisleColumns: number[];
  rows: SeatMapRow[];
}
