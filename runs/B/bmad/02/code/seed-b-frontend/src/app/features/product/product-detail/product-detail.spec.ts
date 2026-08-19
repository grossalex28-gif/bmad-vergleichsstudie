import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap } from '@angular/router';

import { ProductDetail as ProductDetailModel } from '../product.model';
import { ProductDetail as ProductDetailComponent } from './product-detail';

function configureWithId(id: string | null) {
  TestBed.configureTestingModule({
    imports: [ProductDetailComponent],
    providers: [
      provideHttpClient(),
      provideHttpClientTesting(),
      { provide: ActivatedRoute, useValue: { snapshot: { paramMap: convertToParamMap(id ? { id } : {}) } } }
    ]
  });
}

const fullProduct: ProductDetailModel = {
  id: 'PD1',
  name: 'Detailprodukt',
  description: 'Eine Beschreibung',
  category: { id: 'CD', name: 'Detailkategorie' },
  subcategory: { id: 'SD1', name: 'Detailunterkategorie' },
  properties: [
    { name: 'Bauform', value: 'Kompakt' },
    { name: 'Farbe', value: 'Rot' }
  ],
  averageRating: null,
  ratingCount: 0,
  offers: [
    { supplierId: 'LD2', supplierName: 'Lieferant Günstig', price: 19.99 },
    { supplierId: 'LD1', supplierName: 'Lieferant Teuer', price: 29.99 }
  ]
};

describe('ProductDetail', () => {
  let httpMock: HttpTestingController;

  afterEach(() => {
    httpMock.verify();
  });

  it('renders name, description, category/subcategory and all properties after successful load (AC 1)', () => {
    configureWithId('PD1');
    httpMock = TestBed.inject(HttpTestingController);

    const fixture = TestBed.createComponent(ProductDetailComponent);
    fixture.detectChanges();

    const req = httpMock.expectOne('/api/products/PD1');
    req.flush(fullProduct);
    fixture.detectChanges();

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('Detailprodukt');
    expect(text).toContain('Eine Beschreibung');
    expect(text).toContain('Detailkategorie');
    expect(text).toContain('Detailunterkategorie');
    expect(text).toContain('Bauform');
    expect(text).toContain('Kompakt');
    expect(text).toContain('Farbe');
    expect(text).toContain('Rot');
  });

  it('renders the supplier list in the order delivered by the backend (AC 2)', () => {
    configureWithId('PD1');
    httpMock = TestBed.inject(HttpTestingController);

    const fixture = TestBed.createComponent(ProductDetailComponent);
    fixture.detectChanges();

    const req = httpMock.expectOne('/api/products/PD1');
    req.flush(fullProduct);
    fixture.detectChanges();

    const items = Array.from((fixture.nativeElement as HTMLElement).querySelectorAll('li')).map(
      (li) => li.textContent ?? ''
    );
    const guenstigIndex = items.findIndex((t) => t.includes('Lieferant Günstig'));
    const teuerIndex = items.findIndex((t) => t.includes('Lieferant Teuer'));
    expect(guenstigIndex).toBeGreaterThanOrEqual(0);
    expect(teuerIndex).toBeGreaterThan(guenstigIndex);
  });

  it('renders "Noch keine Bewertungen" instead of "0" or an error when ratingCount is 0 (AC 3)', () => {
    configureWithId('PD1');
    httpMock = TestBed.inject(HttpTestingController);

    const fixture = TestBed.createComponent(ProductDetailComponent);
    fixture.detectChanges();

    const req = httpMock.expectOne('/api/products/PD1');
    req.flush({ ...fullProduct, ratingCount: 0, averageRating: null });
    fixture.detectChanges();

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('Noch keine Bewertungen');
  });

  it('renders "3,5 (2 Bewertungen)" for ratingCount 2 and averageRating 3.5 (AC 4)', () => {
    configureWithId('PD1');
    httpMock = TestBed.inject(HttpTestingController);

    const fixture = TestBed.createComponent(ProductDetailComponent);
    fixture.detectChanges();

    const req = httpMock.expectOne('/api/products/PD1');
    req.flush({ ...fullProduct, ratingCount: 2, averageRating: 3.5 });
    fixture.detectChanges();

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('3,5 (2 Bewertungen)');
  });

  it('renders all visible texts in German (AC 5)', () => {
    configureWithId('PD1');
    httpMock = TestBed.inject(HttpTestingController);

    const fixture = TestBed.createComponent(ProductDetailComponent);
    fixture.detectChanges();
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Lädt…');

    const req = httpMock.expectOne('/api/products/PD1');
    req.flush(fullProduct);
    fixture.detectChanges();

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('Noch keine Bewertungen');
    expect(text).toContain('Lieferanten');
  });

  it('renders "Produkt nicht gefunden." on an HTTP 404 response (AC 6)', () => {
    configureWithId('PD1');
    httpMock = TestBed.inject(HttpTestingController);

    const fixture = TestBed.createComponent(ProductDetailComponent);
    fixture.detectChanges();

    const req = httpMock.expectOne('/api/products/PD1');
    req.flush(null, { status: 404, statusText: 'Not Found' });
    fixture.detectChanges();

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('Produkt nicht gefunden.');
  });

  it('renders "Produkt nicht gefunden." without issuing an HTTP request when the id route parameter is missing (AC 6, Edge Case)', () => {
    configureWithId(null);
    httpMock = TestBed.inject(HttpTestingController);

    const fixture = TestBed.createComponent(ProductDetailComponent);
    fixture.detectChanges();

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('Produkt nicht gefunden.');
  });

  it('submits a rating and updates the displayed average/count from the response without refetching (AC 1, 4)', () => {
    configureWithId('PD1');
    httpMock = TestBed.inject(HttpTestingController);

    const fixture = TestBed.createComponent(ProductDetailComponent);
    fixture.detectChanges();
    httpMock.expectOne('/api/products/PD1').flush({ ...fullProduct, ratingCount: 0, averageRating: null });
    fixture.detectChanges();

    const el = fixture.nativeElement as HTMLElement;
    (el.querySelector('input[type="text"]') as HTMLInputElement).value = 'Anna';
    el.querySelector('input[type="text"]')!.dispatchEvent(new Event('input'));
    (el.querySelector('select') as HTMLSelectElement).value = '4';
    el.querySelector('select')!.dispatchEvent(new Event('change'));
    (el.querySelector('button[type="submit"]') as HTMLButtonElement).click();

    const req = httpMock.expectOne('/api/products/PD1/ratings');
    expect(req.request.body).toEqual({ authorName: 'Anna', score: 4 });
    req.flush({ averageRating: 4, ratingCount: 1 });
    fixture.detectChanges();

    expect(el.textContent).toContain('4,0 (1 Bewertungen)');
    expect(el.textContent).toContain('Vielen Dank für Ihre Bewertung!');
    httpMock.expectNone('/api/products/PD1');
  });

  it('shows a validation message and issues no request when author name or score is missing (AC 2)', () => {
    configureWithId('PD1');
    httpMock = TestBed.inject(HttpTestingController);

    const fixture = TestBed.createComponent(ProductDetailComponent);
    fixture.detectChanges();
    httpMock.expectOne('/api/products/PD1').flush(fullProduct);
    fixture.detectChanges();

    const el = fixture.nativeElement as HTMLElement;
    (el.querySelector('button[type="submit"]') as HTMLButtonElement).click();
    fixture.detectChanges();

    expect(el.textContent).toContain('Bitte Sternewert und Namen angeben.');
    httpMock.expectNone('/api/products/PD1/ratings');
  });

  it('shows a German error message on a 400 response and leaves the displayed rating unchanged (AC 2)', () => {
    configureWithId('PD1');
    httpMock = TestBed.inject(HttpTestingController);

    const fixture = TestBed.createComponent(ProductDetailComponent);
    fixture.detectChanges();
    httpMock.expectOne('/api/products/PD1').flush({ ...fullProduct, ratingCount: 0, averageRating: null });
    fixture.detectChanges();

    const el = fixture.nativeElement as HTMLElement;
    (el.querySelector('input[type="text"]') as HTMLInputElement).value = 'Anna';
    el.querySelector('input[type="text"]')!.dispatchEvent(new Event('input'));
    (el.querySelector('select') as HTMLSelectElement).value = '4';
    el.querySelector('select')!.dispatchEvent(new Event('change'));
    (el.querySelector('button[type="submit"]') as HTMLButtonElement).click();

    const req = httpMock.expectOne('/api/products/PD1/ratings');
    req.flush(null, { status: 400, statusText: 'Bad Request' });
    fixture.detectChanges();

    expect(el.textContent).toContain('Bewertung konnte nicht gespeichert werden. Bitte Eingaben prüfen.');
    expect(el.textContent).toContain('Noch keine Bewertungen');
  });

  it('renders the rating form labels in German (AC 6)', () => {
    configureWithId('PD1');
    httpMock = TestBed.inject(HttpTestingController);

    const fixture = TestBed.createComponent(ProductDetailComponent);
    fixture.detectChanges();
    httpMock.expectOne('/api/products/PD1').flush(fullProduct);
    fixture.detectChanges();

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('Bewertung abgeben');
    expect(text).toContain('Ihr Name');
    expect(text).toContain('Sternewert');
  });

  it('adds an item to the cart without an X-Cart-Id header when no cart exists yet, then uses the returned cartId for a second add (AC 1, 7)', () => {
    configureWithId('PD1');
    httpMock = TestBed.inject(HttpTestingController);

    const fixture = TestBed.createComponent(ProductDetailComponent);
    fixture.detectChanges();
    httpMock.expectOne('/api/products/PD1').flush(fullProduct);
    fixture.detectChanges();

    const el = fixture.nativeElement as HTMLElement;
    const firstButton = el.querySelectorAll('button[type="button"]')[0] as HTMLButtonElement;
    firstButton.click();

    const firstReq = httpMock.expectOne('/api/cart/items');
    expect(firstReq.request.headers.has('X-Cart-Id')).toBe(false);
    expect(firstReq.request.body).toEqual({ productId: 'PD1', supplierId: 'LD2', quantity: 1 });
    firstReq.flush({ cartId: 'C1', items: [], totalPrice: 19.99 });
    fixture.detectChanges();

    expect(el.textContent).toContain('Zum Warenkorb hinzugefügt.');

    const secondButton = el.querySelectorAll('button[type="button"]')[1] as HTMLButtonElement;
    secondButton.click();

    const secondReq = httpMock.expectOne('/api/cart/items');
    expect(secondReq.request.headers.get('X-Cart-Id')).toBe('C1');
    secondReq.flush({ cartId: 'C1', items: [], totalPrice: 49.98 });
  });

  it('sends the entered quantity in the add-to-cart request body (AC 3)', () => {
    configureWithId('PD1');
    httpMock = TestBed.inject(HttpTestingController);

    const fixture = TestBed.createComponent(ProductDetailComponent);
    fixture.detectChanges();
    httpMock.expectOne('/api/products/PD1').flush(fullProduct);
    fixture.detectChanges();

    const el = fixture.nativeElement as HTMLElement;
    const qtyInput = el.querySelector('input[type="number"]') as HTMLInputElement;
    qtyInput.value = '4';
    const button = el.querySelectorAll('button[type="button"]')[0] as HTMLButtonElement;
    button.click();

    const req = httpMock.expectOne('/api/cart/items');
    expect(req.request.body).toEqual({ productId: 'PD1', supplierId: 'LD2', quantity: 4 });
    req.flush({ cartId: 'C1', items: [], totalPrice: 0 });
  });

  it('shows a German "offer no longer available" message on a 404 response (AC 5)', () => {
    configureWithId('PD1');
    httpMock = TestBed.inject(HttpTestingController);

    const fixture = TestBed.createComponent(ProductDetailComponent);
    fixture.detectChanges();
    httpMock.expectOne('/api/products/PD1').flush(fullProduct);
    fixture.detectChanges();

    const el = fixture.nativeElement as HTMLElement;
    const button = el.querySelectorAll('button[type="button"]')[0] as HTMLButtonElement;
    button.click();

    const req = httpMock.expectOne('/api/cart/items');
    req.flush(null, { status: 404, statusText: 'Not Found' });
    fixture.detectChanges();

    expect(el.textContent).toContain('Dieses Angebot ist nicht mehr verfügbar.');
  });

  it('shows a validation message and issues no request when the quantity is not a positive integer (AC 4)', () => {
    configureWithId('PD1');
    httpMock = TestBed.inject(HttpTestingController);

    const fixture = TestBed.createComponent(ProductDetailComponent);
    fixture.detectChanges();
    httpMock.expectOne('/api/products/PD1').flush(fullProduct);
    fixture.detectChanges();

    const el = fixture.nativeElement as HTMLElement;
    const qtyInput = el.querySelector('input[type="number"]') as HTMLInputElement;
    qtyInput.value = '0';
    const button = el.querySelectorAll('button[type="button"]')[0] as HTMLButtonElement;
    button.click();
    fixture.detectChanges();

    expect(el.textContent).toContain('Bitte eine gültige Menge (mindestens 1) angeben.');
    httpMock.expectNone('/api/cart/items');
  });

  it('renders the add-to-cart controls in German (AC 8)', () => {
    configureWithId('PD1');
    httpMock = TestBed.inject(HttpTestingController);

    const fixture = TestBed.createComponent(ProductDetailComponent);
    fixture.detectChanges();
    httpMock.expectOne('/api/products/PD1').flush(fullProduct);
    fixture.detectChanges();

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('In den Warenkorb legen');
    expect(text).toContain('Menge');
  });
});
