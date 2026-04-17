import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../api/api.service';

export interface UserPreferences {
  favoriteRoutes: string[];
  displayName: string | null;
  notifyLeihruckgabe: boolean;
  notifyVeranstaltungen: boolean;
  notifyMindestbestand: boolean;
}

export interface UpdateUserPreferences {
  favoriteRoutes: string[];
  displayName: string | null;
  notifyLeihruckgabe: boolean;
  notifyVeranstaltungen: boolean;
  notifyMindestbestand: boolean;
}

@Injectable({ providedIn: 'root' })
export class UserPreferencesService {
  private readonly api = inject(ApiService);

  getPreferences(): Observable<UserPreferences> {
    return this.api.get<UserPreferences>('/users/me/preferences');
  }

  updatePreferences(update: UpdateUserPreferences): Observable<UserPreferences> {
    return this.api.put<UserPreferences>('/users/me/preferences', update);
  }
}
