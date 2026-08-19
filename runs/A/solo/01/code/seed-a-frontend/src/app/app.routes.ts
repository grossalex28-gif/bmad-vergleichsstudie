import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'veranstaltungen' },
  {
    path: 'veranstaltungen',
    loadComponent: () =>
      import('./features/veranstaltungsliste/veranstaltungsliste').then((m) => m.VeranstaltungslisteComponent)
  },
  {
    path: 'veranstaltungen/:id',
    loadComponent: () =>
      import('./features/veranstaltungsdetail/veranstaltungsdetail').then((m) => m.VeranstaltungsdetailComponent)
  },
  {
    path: 'veranstaltungen/:id/sitzplatzwahl',
    loadComponent: () =>
      import('./features/sitzplatzwahl/sitzplatzwahl').then((m) => m.SitzplatzwahlComponent)
  },
  {
    path: 'buchungen',
    loadComponent: () =>
      import('./features/buchung-abrufen/buchung-abrufen').then((m) => m.BuchungAbrufenComponent)
  },
  {
    path: 'buchungen/:referenz',
    loadComponent: () =>
      import('./features/buchungsdetail/buchungsdetail').then((m) => m.BuchungsdetailComponent)
  },
  { path: '**', redirectTo: 'veranstaltungen' }
];
