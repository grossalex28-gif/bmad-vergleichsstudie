export interface ProductSummary {
  id: string;
  name: string;
}

export interface ProductListResponse {
  items: ProductSummary[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export interface SubcategorySummary {
  id: string;
  name: string;
  properties: string[];
}

export interface CategorySummary {
  id: string;
  name: string;
  subcategories: SubcategorySummary[];
}
