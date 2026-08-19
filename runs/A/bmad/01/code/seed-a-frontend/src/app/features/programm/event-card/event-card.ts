import { DatePipe } from '@angular/common';
import { Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-event-card',
  imports: [RouterLink, DatePipe],
  templateUrl: './event-card.html',
  styleUrl: './event-card.scss'
})
export class EventCard {
  readonly id = input.required<string>();
  readonly title = input.required<string>();
  readonly venueName = input.required<string>();
  readonly startsAt = input.required<string>();
}
