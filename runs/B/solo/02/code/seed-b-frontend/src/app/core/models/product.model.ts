export interface ProductListItem {
  id: number;
  name: string;
  subcategoryId: number;
  subcategoryName: string;
  categoryId: number;
  categoryName: string;
  minPrice: number | null;
  averageRating: number | null;
  ratingCount: number;
  viewCount: number;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface ProductOffer {
  supplierId: number;
  supplierName: string;
  price: number;
}

export interface ProductDetail {
  id: number;
  name: string;
  description: string;
  subcategoryId: number;
  subcategoryName: string;
  categoryId: number;
  categoryName: string;
  properties: Record<string, string>;
  averageRating: number | null;
  ratingCount: number;
  viewCount: number;
  offers: ProductOffer[];
}

export interface ReviewCreate {
  authorName: string;
  rating: number;
}

export interface ReviewResult {
  averageRating: number;
  ratingCount: number;
}

export type SortBy = 'name' | 'price' | 'popularity';
export type SortDir = 'asc' | 'desc';

export interface ProductQuery {
  categoryId?: number;
  subcategoryId?: number;
  search?: string;
  sortBy?: SortBy;
  sortDir?: SortDir;
  page?: number;
  properties?: Record<string, string>;
}
