import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { App } from './app';

describe('App', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [provideRouter([])]
    }).compileComponents();
  });

  it('should create the app', () => {
    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance;
    expect(app).toBeTruthy();
  });

  it('rendert den Header mit dem Link "Buchung abrufen" und ein router-outlet', () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;

    const buchungLink = Array.from(compiled.querySelectorAll('a')).find(
      a => a.textContent?.trim() === 'Buchung abrufen'
    );
    expect(buchungLink).toBeTruthy();
    expect(buchungLink?.getAttribute('href')).toBe('/buchung-abrufen');
    expect(compiled.querySelector('router-outlet')).toBeTruthy();
  });
});
