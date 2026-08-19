import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { Buchung } from '../../core/models/buchung.model';
import { BuchungsdetailComponent } from './buchungsdetail';

const buchung: Buchung = {
  referenz: 'ABC12345',
  name: 'Erika Mustermann',
  email: 'erika@example.com',
  status: 'Aktiv',
  erstelltAm: '2026-08-01T10:00:00',
  veranstaltungId: 'E1',
  veranstaltungTitel: 'Testkonzert',
  veranstaltungZeitpunkt: '2026-09-05T19:30:00',
  spielstaetteName: 'Stadthalle',
  raumName: 'Großer Saal',
  positionen: [
    { reihe: 'A', spalte: 1, preiskategorieId: 'E1-A', preiskategorieName: 'Kategorie A', preis: 30 }
  ],
  gesamtpreis: 30
};

describe('BuchungsdetailComponent', () => {
  let httpMock: HttpTestingController;

  async function erstelleComponent() {
    await TestBed.configureTestingModule({
      imports: [BuchungsdetailComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: convertToParamMap({ referenz: 'ABC12345' }) } }
        }
      ]
    }).compileComponents();

    httpMock = TestBed.inject(HttpTestingController);
    const fixture = TestBed.createComponent(BuchungsdetailComponent);
    fixture.detectChanges();
    httpMock.expectOne('/api/buchungen/ABC12345').flush(buchung);
    fixture.detectChanges();
    return fixture;
  }

  afterEach(() => httpMock.verify());

  it('zeigt Positionen und Gesamtpreis einer Buchung an', async () => {
    const fixture = await erstelleComponent();
    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('ABC12345');
    expect(text).toContain('Testkonzert');
    expect(text).toContain('A1');
  });

  it('storniert eine aktive Buchung nach Bestätigung und zeigt den neuen Status', async () => {
    vi.spyOn(window, 'confirm').mockReturnValue(true);
    const fixture = await erstelleComponent();

    fixture.componentInstance['stornieren']();
    httpMock.expectOne('/api/buchungen/ABC12345/stornieren').flush({ ...buchung, status: 'Storniert' });
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('storniert');
    expect(fixture.nativeElement.querySelector('button')).toBeNull();
  });

  it('storniert nicht, wenn der Nutzer die Bestätigung ablehnt', async () => {
    vi.spyOn(window, 'confirm').mockReturnValue(false);
    const fixture = await erstelleComponent();

    fixture.componentInstance['stornieren']();
    httpMock.expectNone('/api/buchungen/ABC12345/stornieren');
  });
});
