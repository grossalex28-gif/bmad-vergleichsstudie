import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', redirectTo: 'events', pathMatch: 'full' },
  {
    path: 'events',
    loadComponent: () => import('./features/event-list/event-list.component').then((m) => m.EventListComponent)
  },
  {
    path: 'events/:id',
    loadComponent: () => import('./features/event-detail/event-detail.component').then((m) => m.EventDetailComponent)
  },
  {
    path: 'events/:id/seats',
    loadComponent: () =>
      import('./features/seat-selection/seat-selection.component').then((m) => m.SeatSelectionComponent)
  },
  {
    path: 'bookings',
    loadComponent: () =>
      import('./features/booking-lookup/booking-lookup.component').then((m) => m.BookingLookupComponent)
  },
  {
    path: 'bookings/:reference',
    loadComponent: () =>
      import('./features/booking-detail/booking-detail.component').then((m) => m.BookingDetailComponent)
  },
  { path: '**', redirectTo: 'events' }
];
