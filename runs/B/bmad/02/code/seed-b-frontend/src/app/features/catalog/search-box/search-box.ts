import { Component, input, output } from '@angular/core';

@Component({
  selector: 'app-search-box',
  templateUrl: './search-box.html'
})
export class SearchBox {
  disabled = input(false);
  searchChanged = output<string | null>();

  protected onSubmit(inputEl: HTMLInputElement): void {
    const trimmed = inputEl.value.trim();
    inputEl.value = trimmed;
    this.searchChanged.emit(trimmed ? trimmed : null);
  }

  protected onNativeClear(inputEl: HTMLInputElement): void {
    if (inputEl.value === '') {
      this.searchChanged.emit(null);
    }
  }
}
