export type Eigenschaften = Record<string, string | number | boolean | null>;

export interface ProductListItem {
  id: string;
  name: string;
  beschreibung: string;
  kategorieId: string;
  kategorieName: string;
  unterkategorieId: string;
  unterkategorieName: string;
  eigenschaften: Eigenschaften;
  minPreis: number | null;
  durchschnittsBewertung: number | null;
  anzahlBewertungen: number;
  aufrufe: number;
}

export interface Offer {
  lieferantId: string;
  lieferantName: string;
  preis: number;
}

export interface ProductDetail {
  id: string;
  name: string;
  beschreibung: string;
  kategorieId: string;
  kategorieName: string;
  unterkategorieId: string;
  unterkategorieName: string;
  eigenschaften: Eigenschaften;
  durchschnittsBewertung: number | null;
  anzahlBewertungen: number;
  aufrufe: number;
  angebote: Offer[];
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export type SortOption = 'name_asc' | 'name_desc' | 'price_asc' | 'price_desc' | 'views_asc' | 'views_desc';

export interface RatingCreate {
  autorName: string;
  wert: number;
}

export interface RatingResponse {
  durchschnittsBewertung: number;
  anzahlBewertungen: number;
}
