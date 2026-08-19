export type ProductSort = 'NameAsc' | 'NameDesc' | 'PriceAsc' | 'PriceDesc' | 'PopularityAsc' | 'PopularityDesc';

export interface ProductListItem {
  id: string;
  name: string;
  subCategoryId: string;
  subCategoryName: string;
  categoryId: string;
  categoryName: string;
  minPrice: number;
  viewCount: number;
  averageRating: number;
  ratingCount: number;
}

export interface ProductListResponse {
  items: ProductListItem[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface Offer {
  supplierId: string;
  supplierName: string;
  price: number;
}

export interface ProductDetail {
  id: string;
  name: string;
  description: string;
  subCategoryId: string;
  subCategoryName: string;
  categoryId: string;
  categoryName: string;
  properties: Record<string, string>;
  averageRating: number;
  ratingCount: number;
  viewCount: number;
  offers: Offer[];
}

export interface ProductQuery {
  categoryId?: string;
  subCategoryId?: string;
  search?: string;
  sort?: ProductSort;
  page?: number;
  properties?: Record<string, string>;
}

export interface CreateRatingRequest {
  authorName: string;
  stars: number;
}

export interface Rating {
  authorName: string;
  stars: number;
  createdAt: string;
  averageRating: number;
  ratingCount: number;
}
