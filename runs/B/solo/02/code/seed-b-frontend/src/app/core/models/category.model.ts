export interface Subcategory {
  id: number;
  name: string;
  propertyNames: string[];
}

export interface Category {
  id: number;
  name: string;
  subcategories: Subcategory[];
}
