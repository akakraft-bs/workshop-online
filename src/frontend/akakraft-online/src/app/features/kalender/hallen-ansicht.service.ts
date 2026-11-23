import { Injectable, signal } from '@angular/core';

export type HallenAnsicht = 'neu' | 'legacy';

const STORAGE_KEY = 'akakraft.hallenbelegung.ansicht';

/**
 * Merkt sich pro Gerät/Browser, ob die neue Hallenbelegung (V2) oder die alte
 * Wochenansicht angezeigt wird. Standard ist die neue Ansicht.
 */
@Injectable({ providedIn: 'root' })
export class HallenAnsichtService {
  readonly ansicht = signal<HallenAnsicht>(lesen());

  setzen(ansicht: HallenAnsicht): void {
    this.ansicht.set(ansicht);
    try {
      if (ansicht === 'legacy') localStorage.setItem(STORAGE_KEY, 'legacy');
      else localStorage.removeItem(STORAGE_KEY);
    } catch {
      // Privater Modus / Storage blockiert: gilt dann nur für diese Sitzung.
    }
  }
}

function lesen(): HallenAnsicht {
  try {
    return localStorage.getItem(STORAGE_KEY) === 'legacy' ? 'legacy' : 'neu';
  } catch {
    return 'neu';
  }
}
