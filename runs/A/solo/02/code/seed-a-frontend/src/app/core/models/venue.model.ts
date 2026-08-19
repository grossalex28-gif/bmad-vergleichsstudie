export interface RoomSummary {
  id: string;
  name: string;
}

export interface Venue {
  id: string;
  name: string;
  rooms: RoomSummary[];
}
