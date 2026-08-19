import { HttpErrorResponse } from '@angular/common/http';

export function fehlermeldung(fehler: unknown, standard: string): string {
  if (fehler instanceof HttpErrorResponse) {
    const nachricht = (fehler.error as { message?: string } | null)?.message;
    if (nachricht) {
      return nachricht;
    }
  }
  return standard;
}
