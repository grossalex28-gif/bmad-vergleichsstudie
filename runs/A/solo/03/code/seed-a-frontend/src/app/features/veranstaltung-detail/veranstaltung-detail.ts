import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { VeranstaltungenService } from '../../core/services/veranstaltungen.service';
import { VeranstaltungDetail } from '../../core/models/api.models';

@Component({
  selector: 'app-veranstaltung-detail',
  imports: [CommonModule, RouterLink],
  templateUrl: './veranstaltung-detail.html',
  styleUrl: './veranstaltung-detail.scss'
})
export class VeranstaltungDetailComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly veranstaltungenService = inject(VeranstaltungenService);

  readonly veranstaltung = signal<VeranstaltungDetail | null>(null);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);

  constructor() {
    const id = this.route.snapshot.paramMap.get('id')!;
    this.veranstaltungenService.getDetail(id).subscribe({
      next: (veranstaltung) => {
        this.veranstaltung.set(veranstaltung);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Diese Veranstaltung konnte nicht gefunden werden.');
        this.loading.set(false);
      }
    });
  }
}
