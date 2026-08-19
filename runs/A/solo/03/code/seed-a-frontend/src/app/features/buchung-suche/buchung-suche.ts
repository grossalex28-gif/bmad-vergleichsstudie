import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';

@Component({
  selector: 'app-buchung-suche',
  imports: [FormsModule],
  templateUrl: './buchung-suche.html',
  styleUrl: './buchung-suche.scss'
})
export class BuchungSuche {
  private readonly router = inject(Router);

  referenz = '';

  suchen(): void {
    const referenz = this.referenz.trim();
    if (!referenz) {
      return;
    }
    this.router.navigate(['/buchungen', referenz]);
  }
}
