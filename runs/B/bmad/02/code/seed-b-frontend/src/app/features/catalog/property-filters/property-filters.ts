import { Component, effect, ElementRef, input, output, viewChild } from '@angular/core';

@Component({
  selector: 'app-property-filters',
  templateUrl: './property-filters.html'
})
export class PropertyFilters {
  properties = input.required<string[]>();
  disabled = input(false);
  filtersChanged = output<Record<string, string> | null>();

  private readonly filterFormRef = viewChild<ElementRef<HTMLFormElement>>('filterForm');

  constructor() {
    // Clears any unsubmitted text left in a reused input when the property
    // set changes (e.g. switching subcategories) so stale values from a
    // previous subcategory can't be silently submitted for the new one.
    effect(() => {
      this.properties();
      this.filterFormRef()?.nativeElement.reset();
    });
  }

  protected onSubmit(formEl: HTMLFormElement): void {
    const formData = new FormData(formEl);
    const filters: Record<string, string> = {};
    for (const name of this.properties()) {
      const value = (formData.get(name) as string | null)?.trim();
      if (value) {
        filters[name] = value;
      }
    }
    this.filtersChanged.emit(Object.keys(filters).length > 0 ? filters : null);
  }
}
