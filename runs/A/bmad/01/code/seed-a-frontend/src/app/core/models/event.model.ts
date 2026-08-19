export interface EventListItem {
  id: string;
  title: string;
  venueName: string;
  startsAt: string;
}

export interface EventDetail {
  id: string;
  title: string;
  description: string;
  durationMinutes: number;
  ageRating: number;
  venueName: string;
  roomName: string;
  startsAt: string;
}

export interface EventListFilter {
  from?: string;
  to?: string;
  venueId?: string;
}
