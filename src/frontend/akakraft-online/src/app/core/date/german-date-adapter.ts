import { Injectable, inject } from '@angular/core';
import { NativeDateAdapter, MAT_DATE_LOCALE } from '@angular/material/core';
import { Platform } from '@angular/cdk/platform';

/** Extends NativeDateAdapter to correctly parse German date strings (dd.MM.yyyy). */
@Injectable()
export class GermanDateAdapter extends NativeDateAdapter {
  constructor() {
    super(inject(MAT_DATE_LOCALE, { optional: true }) ?? 'de-DE', inject(Platform));
  }

  override parse(value: unknown): Date | null {
    if (typeof value === 'string') {
      const trimmed = value.trim();
      // Match dd.MM.yyyy, d.M.yyyy, dd.M.yy, etc.
      const match = trimmed.match(/^(\d{1,2})\.(\d{1,2})\.(\d{2,4})$/);
      if (match) {
        let day   = parseInt(match[1], 10);
        let month = parseInt(match[2], 10) - 1;
        let year  = parseInt(match[3], 10);
        if (year < 100) year += 2000;
        const date = new Date(year, month, day);
        if (
          date.getFullYear() === year &&
          date.getMonth()    === month &&
          date.getDate()     === day
        ) {
          return date;
        }
        return null;
      }
    }
    return super.parse(value);
  }

  override format(date: Date, displayFormat: object): string {
    if (!this.isValid(date)) return '';
    const pad = (n: number) => String(n).padStart(2, '0');
    return `${pad(date.getDate())}.${pad(date.getMonth() + 1)}.${date.getFullYear()}`;
  }
}
