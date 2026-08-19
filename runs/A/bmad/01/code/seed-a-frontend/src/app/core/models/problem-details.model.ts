export interface ValidationProblemDetails {
  status?: number;
  errors?: Record<string, string[]>;
}

export interface ConflictProblemDetails {
  status?: number;
  conflictingSeats?: string[];
}
