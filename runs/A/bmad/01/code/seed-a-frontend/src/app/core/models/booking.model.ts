export interface CreateBookingSeatRequest {
  seatId: string;
  priceCategoryId: string;
}

export interface CreateBookingRequest {
  eventId: string;
  name: string;
  email: string;
  seats: CreateBookingSeatRequest[];
}

export interface BookingSeatDetail {
  seatId: string;
  row: string;
  column: number;
  priceCategoryId: string;
  priceCategoryName: string;
  price: number;
}

export interface Booking {
  reference: string;
  eventId: string;
  eventTitle: string;
  venueName: string;
  startsAt: string;
  status: 'Active' | 'Cancelled';
  seats: BookingSeatDetail[];
  totalPrice: number;
}
