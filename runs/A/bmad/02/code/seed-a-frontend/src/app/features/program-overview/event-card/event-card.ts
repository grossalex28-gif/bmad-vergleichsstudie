import { Component, input } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';

import { EventSummary } from '../../../core/api/event-summary';

@Component({
  selector: 'app-event-card',
  imports: [RouterLink, DatePipe],
  templateUrl: './event-card.html',
  styleUrl: './event-card.scss'
})
export class EventCard {
  readonly event = input.required<EventSummary>();
}
