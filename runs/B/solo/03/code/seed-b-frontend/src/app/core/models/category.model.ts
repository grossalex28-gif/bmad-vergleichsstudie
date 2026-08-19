export interface SubCategory {
  id: string;
  name: string;
  propertyNames: string[];
}

export interface Category {
  id: string;
  name: string;
  subCategories: SubCategory[];
}
