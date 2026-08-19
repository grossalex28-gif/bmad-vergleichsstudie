import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./features/veranstaltungen-liste/veranstaltungen-liste').then((m) => m.VeranstaltungenListe)
  },
  {
    path: 'veranstaltungen/:id',
    loadComponent: () =>
      import('./features/veranstaltung-detail/veranstaltung-detail').then((m) => m.VeranstaltungDetailComponent)
  },
  {
    path: 'veranstaltungen/:id/sitzplan',
    loadComponent: () =>
      import('./features/sitzplatzauswahl/sitzplatzauswahl').then((m) => m.Sitzplatzauswahl)
  },
  {
    path: 'buchungen',
    loadComponent: () => import('./features/buchung-suche/buchung-suche').then((m) => m.BuchungSuche)
  },
  {
    path: 'buchungen/:referenz',
    loadComponent: () => import('./features/buchung-anzeige/buchung-anzeige').then((m) => m.BuchungAnzeige)
  },
  {
    path: '**',
    redirectTo: ''
  }
];
