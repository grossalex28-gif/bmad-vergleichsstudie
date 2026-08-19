export interface SubcategoryNavItem {
  id: string;
  name: string;
}

export interface CategoryNavItem {
  id: string;
  name: string;
  subcategories: SubcategoryNavItem[];
}

export interface CategorySelection {
  categoryId: string | null;
  subcategoryId: string | null;
}
