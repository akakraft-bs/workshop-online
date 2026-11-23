import { Component, inject } from '@angular/core';
import { HallenAnsichtService } from './hallen-ansicht.service';
import { KalenderPageComponent } from './kalender-page.component';
import { HallenbelegungV2Component } from './v2/hallenbelegung-v2.component';

/** Einstieg /kalender: zeigt die neue Hallenbelegung oder – falls gewählt – die alte Wochenansicht. */
@Component({
  selector: 'app-hallenbelegung-page',
  imports: [KalenderPageComponent, HallenbelegungV2Component],
  template: `
    @if (ansicht.ansicht() === 'legacy') {
      <app-kalender-page />
    } @else {
      <app-hallenbelegung-v2 />
    }
  `,
  styles: [`:host { display: flex; flex-direction: column; height: 100%; }`],
})
export class HallenbelegungPageComponent {
  readonly ansicht = inject(HallenAnsichtService);
}
