import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { App } from './app';

describe('App', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [provideRouter([])],
    }).compileComponents();
  });

  it('should create the app', () => {
    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance;
    expect(app).toBeTruthy();
  });

  it('zeigt die globale Navigation mit Logo- und "Buchung abrufen"-Link', () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();

    const logo: HTMLAnchorElement = fixture.nativeElement.querySelector('.app-nav__logo');
    const link: HTMLAnchorElement = fixture.nativeElement.querySelector('.app-nav__link');

    expect(logo.getAttribute('routerLink')).toBe('/');
    expect(link.getAttribute('routerLink')).toBe('/buchung-abrufen');
    expect(link.textContent).toContain('Buchung abrufen');
  });
});
