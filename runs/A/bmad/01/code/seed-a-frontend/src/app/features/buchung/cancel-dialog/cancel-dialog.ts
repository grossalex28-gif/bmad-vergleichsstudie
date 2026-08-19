import { Component, HostListener, input, output } from '@angular/core';

@Component({
  selector: 'app-cancel-dialog',
  imports: [],
  templateUrl: './cancel-dialog.html',
  styleUrl: './cancel-dialog.scss'
})
export class CancelDialog {
  readonly cancelling = input<boolean>(false);
  readonly confirm = output<void>();
  readonly cancel = output<void>();

  @HostListener('document:keydown.escape')
  protected onEscape(): void {
    if (this.cancelling()) {
      return;
    }
    this.cancel.emit();
  }
}
