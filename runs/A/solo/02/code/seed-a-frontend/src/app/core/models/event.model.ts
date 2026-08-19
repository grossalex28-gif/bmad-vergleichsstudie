export interface EventListItem {
  id: string;
  title: string;
  venueId: string;
  venueName: string;
  startsAt: string;
}

export interface PriceCategory {
  id: string;
  name: string;
  price: number;
}

export interface EventDetail {
  id: string;
  title: string;
  description: string;
  durationMinutes: number;
  ageRating: number;
  venueId: string;
  venueName: string;
  roomId: string;
  roomName: string;
  startsAt: string;
  priceCategories: PriceCategory[];
}
