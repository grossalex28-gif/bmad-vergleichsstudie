import { Routes } from '@angular/router';

import { EventListPage } from './features/programm/event-list-page/event-list-page';
import { EventDetailPage } from './features/veranstaltung/event-detail-page/event-detail-page';
import { SitzplanPage } from './features/sitzplan/sitzplan-page/sitzplan-page';
import { BuchungDetailPage } from './features/buchung/buchung-detail-page/buchung-detail-page';
import { BuchungAbrufenPage } from './features/buchung/buchung-abrufen-page/buchung-abrufen-page';

export const routes: Routes = [
  { path: '', component: EventListPage },
  { path: 'veranstaltungen/:id', component: EventDetailPage },
  { path: 'veranstaltungen/:id/sitzplan', component: SitzplanPage },
  { path: 'buchungen', component: BuchungAbrufenPage },
  { path: 'buchungen/:referenz', component: BuchungDetailPage }
];
