import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../api/api.service';

export interface UserPreferences {
  favoriteRoutes: string[];
}

@Injectable({ providedIn: 'root' })
export class UserPreferencesService {
  private readonly api = inject(ApiService);

  getPreferences(): Observable<UserPreferences> {
    return this.api.get<UserPreferences>('/users/me/preferences');
  }

  updatePreferences(favoriteRoutes: string[]): Observable<UserPreferences> {
    return this.api.put<UserPreferences>('/users/me/preferences', { favoriteRoutes });
  }
}
