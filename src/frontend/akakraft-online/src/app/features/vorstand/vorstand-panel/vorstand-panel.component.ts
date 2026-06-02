import { Component } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-vorstand-panel',
  imports: [MatCardModule, MatButtonModule, MatIconModule, RouterLink],
  templateUrl: './vorstand-panel.component.html',
  styleUrl: './vorstand-panel.component.scss',
})
export class VorstandPanelComponent {}
