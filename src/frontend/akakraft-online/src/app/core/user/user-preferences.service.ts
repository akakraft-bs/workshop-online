import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../api/api.service';

export interface UserPreferences {
  favoriteRoutes: string[];
  displayName: string | null;
}

@Injectable({ providedIn: 'root' })
export class UserPreferencesService {
  private readonly api = inject(ApiService);

  getPreferences(): Observable<UserPreferences> {
    return this.api.get<UserPreferences>('/users/me/preferences');
  }

  updatePreferences(favoriteRoutes: string[], displayName: string | null): Observable<UserPreferences> {
    return this.api.put<UserPreferences>('/users/me/preferences', { favoriteRoutes, displayName });
  }
}
