import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { BuchungAbrufenComponent } from './buchung-abrufen';

describe('BuchungAbrufenComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [BuchungAbrufenComponent],
      providers: [provideRouter([])]
    }).compileComponents();
  });

  it('navigiert bei gültiger Referenz zur Buchungsdetailseite (großgeschrieben)', () => {
    const fixture = TestBed.createComponent(BuchungAbrufenComponent);
    const router = TestBed.inject(Router);
    const navigateSpy = vi.spyOn(router, 'navigate').mockResolvedValue(true);

    fixture.componentInstance['referenz'] = ' ab12cd34 ';
    fixture.componentInstance['abrufen']();

    expect(navigateSpy).toHaveBeenCalledWith(['/buchungen', 'AB12CD34']);
  });

  it('zeigt einen Fehler, wenn keine Referenz eingegeben wurde', () => {
    const fixture = TestBed.createComponent(BuchungAbrufenComponent);
    const router = TestBed.inject(Router);
    const navigateSpy = vi.spyOn(router, 'navigate').mockResolvedValue(true);

    fixture.componentInstance['referenz'] = '   ';
    fixture.componentInstance['abrufen']();

    expect(navigateSpy).not.toHaveBeenCalled();
    expect(fixture.componentInstance['fehler']()).toBeTruthy();
  });
});
