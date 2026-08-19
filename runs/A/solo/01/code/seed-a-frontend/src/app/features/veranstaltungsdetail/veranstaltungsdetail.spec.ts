import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { VeranstaltungDetail } from '../../core/models/veranstaltung.model';
import { VeranstaltungsdetailComponent } from './veranstaltungsdetail';

const veranstaltung: VeranstaltungDetail = {
  id: 'E1',
  titel: 'Testkonzert',
  beschreibung: 'Eine Beschreibung.',
  dauerMinuten: 90,
  altersfreigabe: 16,
  spielstaetteId: 'V1',
  spielstaetteName: 'Stadthalle',
  raumId: 'R1',
  raumName: 'Großer Saal',
  zeitpunkt: '2026-09-05T19:30:00',
  preiskategorien: [{ id: 'E1-A', name: 'Kategorie A', preis: 30 }]
};

describe('VeranstaltungsdetailComponent', () => {
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [VeranstaltungsdetailComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: convertToParamMap({ id: 'E1' }) } }
        }
      ]
    }).compileComponents();

    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('lädt und zeigt die Veranstaltungsdetails inklusive Altersfreigabe und Preiskategorien', () => {
    const fixture = TestBed.createComponent(VeranstaltungsdetailComponent);
    fixture.detectChanges();

    httpMock.expectOne('/api/veranstaltungen/E1').flush(veranstaltung);
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('Testkonzert');
    expect(text).toContain('Großer Saal');
    expect(text).toContain('Ab 16 Jahren');
    expect(text).toContain('Kategorie A');
  });

  it('zeigt eine Fehlermeldung, wenn die Veranstaltung nicht gefunden wird', () => {
    const fixture = TestBed.createComponent(VeranstaltungsdetailComponent);
    fixture.detectChanges();

    httpMock
      .expectOne('/api/veranstaltungen/E1')
      .flush({ message: 'Nicht gefunden' }, { status: 404, statusText: 'Not Found' });
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.fehler')?.textContent).toContain('Nicht gefunden');
  });
});
