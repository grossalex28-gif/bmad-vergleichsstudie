export interface Subcategory {
  id: string;
  name: string;
  eigenschaften: string[];
}

export interface Category {
  id: string;
  name: string;
  unterkategorien: Subcategory[];
}
