import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../api/api.service';

export interface NotificationPreferences {
  werkzeugRueckgabe: boolean;
  veranstaltungen: boolean;
  verbrauchsmaterialMindestbestand: boolean;
}

@Injectable({ providedIn: 'root' })
export class NotificationPreferencesService {
  private readonly api = inject(ApiService);

  getPreferences(): Observable<NotificationPreferences> {
    return this.api.get<NotificationPreferences>('/users/me/notification-preferences');
  }

  updatePreferences(prefs: NotificationPreferences): Observable<NotificationPreferences> {
    return this.api.put<NotificationPreferences>('/users/me/notification-preferences', prefs);
  }
}
