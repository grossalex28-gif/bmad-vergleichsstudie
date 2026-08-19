import { Component, input, signal } from '@angular/core';

@Component({
  selector: 'app-buchungsreferenz',
  imports: [],
  templateUrl: './buchungsreferenz.html',
  styleUrl: './buchungsreferenz.scss',
})
export class Buchungsreferenz {
  readonly referenz = input.required<string>();

  protected readonly kopiert = signal(false);

  protected async kopieren(): Promise<void> {
    try {
      await navigator.clipboard.writeText(this.referenz());
      this.kopiert.set(true);
      setTimeout(() => this.kopiert.set(false), 2000);
    } catch {
      // Clipboard-API kann in unsicheren Kontexten/älteren Browsern fehlen — die Referenz
      // bleibt sichtbar und lässt sich manuell markieren, kein Fallback-Mechanismus nötig.
    }
  }
}
