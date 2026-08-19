export interface ProductListItem {
  id: string;
  name: string;
  subcategoryName: string;
  minPrice: number;
}

export interface AttributeFilterOption {
  name: string;
  values: string[];
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  availableAttributeFilters?: AttributeFilterOption[];
}

export type ProductSortBy = 'name' | 'price' | 'views';
export type ProductSortDirection = 'asc' | 'desc';

export interface ProductAttribute {
  name: string;
  value: string;
}

export interface ProductOffer {
  supplierId: string;
  supplierName: string;
  price: number;
}

export interface Product {
  id: string;
  name: string;
  description: string;
  categoryId: string;
  categoryName: string;
  subcategoryId: string;
  subcategoryName: string;
  attributes: ProductAttribute[];
  averageRating: number | null;
  ratingCount: number;
  offers: ProductOffer[];
}

export interface RatingSummary {
  averageRating: number | null;
  ratingCount: number;
}
