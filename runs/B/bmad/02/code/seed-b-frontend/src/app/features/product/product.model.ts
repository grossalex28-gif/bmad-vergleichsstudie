export interface ProductProperty {
  name: string;
  value: string;
}

export interface ProductOffer {
  supplierId: string;
  supplierName: string;
  price: number;
}

export interface ProductDetail {
  id: string;
  name: string;
  description: string;
  category: { id: string; name: string };
  subcategory: { id: string; name: string };
  properties: ProductProperty[];
  averageRating: number | null;
  ratingCount: number;
  offers: ProductOffer[];
}

export interface RatingSubmission {
  authorName: string;
  score: number;
}

export interface RatingSubmissionResult {
  averageRating: number | null;
  ratingCount: number;
}
