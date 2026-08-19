import { Routes } from '@angular/router';
import { BuchungAbrufen } from './buchung-abrufen/buchung-abrufen';
import { Buchungsdetail } from './buchung-abrufen/buchungsdetail';
import { Buchung } from './buchung/buchung';
import { Programmuebersicht } from './programmuebersicht/programmuebersicht';
import { Veranstaltungsdetail } from './veranstaltungsdetail/veranstaltungsdetail';

export const routes: Routes = [
  { path: '', component: Programmuebersicht },
  { path: 'buchung-abrufen', component: BuchungAbrufen },
  { path: 'buchungen/:referenz', component: Buchungsdetail },
  { path: 'veranstaltungen/:id/buchung', component: Buchung },
  { path: 'veranstaltungen/:id', component: Veranstaltungsdetail },
];
