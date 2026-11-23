import { inject } from '@angular/core';
import { MatIconRegistry } from '@angular/material/icon';
import { DomSanitizer } from '@angular/platform-browser';

/** Eigene Icons im Material-Stil (24×24, 2 px Strich, currentColor). */
const CUSTOM_ICONS: Record<string, string> = {
  trailer: `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M4.5 15H3V7h13v8M14.5 15H22M20 15v3"/><circle cx="7" cy="15" r="2.5"/><circle cx="12" cy="15" r="2.5"/></svg>`,
};

/** Registriert die eigenen Icons inline – kein HTTP-Request, funktioniert offline. */
export function registerCustomIcons(): void {
  const registry = inject(MatIconRegistry);
  const sanitizer = inject(DomSanitizer);
  for (const [name, svg] of Object.entries(CUSTOM_ICONS)) {
    registry.addSvgIconLiteral(name, sanitizer.bypassSecurityTrustHtml(svg));
  }
}
