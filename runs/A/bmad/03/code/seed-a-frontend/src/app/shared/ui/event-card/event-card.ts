import { DatePipe } from '@angular/common';
import { Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Veranstaltung } from '../../models/veranstaltung';
import { spielstaettenOffset } from '../../utils/zeitzone';

@Component({
  selector: 'app-event-card',
  imports: [DatePipe, RouterLink],
  templateUrl: './event-card.html',
  styleUrl: './event-card.scss'
})
export class EventCard {
  readonly veranstaltung = input.required<Veranstaltung>();

  protected readonly spielstaettenOffset = spielstaettenOffset;
}
