import { Component, effect, inject, input, output, signal } from '@angular/core';

import { PropertyFilterOption, SubcategoriesApi } from '../../../core/api/subcategories.api';

export type PropertyFilterSelection = Record<string, string[]>;

@Component({
  selector: 'app-property-filter',
  imports: [],
  templateUrl: './property-filter.html',
  styleUrl: './property-filter.scss',
})
export class PropertyFilter {
  private readonly subcategoriesApi = inject(SubcategoriesApi);

  readonly subcategoryId = input<string | null>(null);
  readonly selection = input<PropertyFilterSelection>({});
  readonly disabled = input(false);
  readonly selectionChange = output<PropertyFilterSelection>();

  protected readonly options = signal<PropertyFilterOption[]>([]);
  protected readonly loading = signal(false);
  protected readonly error = signal(false);

  constructor() {
    effect(() => {
      const subcategoryId = this.subcategoryId();
      if (subcategoryId === null) {
        this.options.set([]);
        return;
      }

      this.loading.set(true);
      this.error.set(false);
      this.subcategoriesApi.getProperties(subcategoryId).subscribe({
        next: (options) => {
          if (this.subcategoryId() !== subcategoryId) {
            return;
          }
          this.options.set(options);
          this.loading.set(false);
        },
        error: () => {
          if (this.subcategoryId() !== subcategoryId) {
            return;
          }
          this.options.set([]);
          this.loading.set(false);
          this.error.set(true);
        },
      });
    });
  }

  protected isChecked(name: string, value: string): boolean {
    return this.selection()[name]?.includes(value) ?? false;
  }

  protected toggleValue(name: string, value: string, checked: boolean): void {
    const next: PropertyFilterSelection = { ...this.selection() };
    const values = next[name] ?? [];

    if (checked) {
      next[name] = [...values, value];
    } else {
      const remaining = values.filter((v) => v !== value);
      if (remaining.length > 0) {
        next[name] = remaining;
      } else {
        delete next[name];
      }
    }

    this.selectionChange.emit(next);
  }
}
