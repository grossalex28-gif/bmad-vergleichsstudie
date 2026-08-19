export interface CreateBookingSeat {
  row: string;
  column: number;
  priceCategoryId: string;
}

export interface CreateBookingRequest {
  eventId: string;
  customerName: string;
  customerEmail: string;
  seats: CreateBookingSeat[];
}

export interface BookingSeat {
  row: string;
  column: number;
  priceCategoryName: string;
  price: number;
}

export interface Booking {
  reference: string;
  eventId: string;
  eventTitle: string;
  eventStartsAt: string;
  customerName: string;
  customerEmail: string;
  createdAt: string;
  isCancelled: boolean;
  totalPrice: number;
  seats: BookingSeat[];
}

export interface BookingConflict {
  message: string;
  conflictingSeats: { row: string; column: number }[];
}
