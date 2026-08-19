import { Routes } from '@angular/router';

import { ProgramOverview } from './features/program-overview/program-overview';
import { BookingLookup } from './features/booking-lookup/booking-lookup';
import { EventDetailPage } from './features/event-detail/event-detail';
import { BookingPage } from './features/booking/booking';
import { BookingDetailPage } from './features/booking-detail/booking-detail';

export const routes: Routes = [
  { path: '', component: ProgramOverview },
  { path: 'buchung-abrufen', component: BookingLookup },
  { path: 'veranstaltungen/:id', component: EventDetailPage },
  { path: 'veranstaltungen/:id/buchung', component: BookingPage },
  { path: 'buchungen/:reference', component: BookingDetailPage }
];
