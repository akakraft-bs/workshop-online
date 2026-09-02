import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../api/api.service';
import {
  ParkAccountStatus,
  ParkAccountUpdateRequest,
  ParkCheckinRequest,
  ParkClaim,
  ParkHistorieEintrag,
  ParkOverview,
} from '../../models/parkplatz.model';

@Injectable({ providedIn: 'root' })
export class ParkplatzService {
  private readonly api = inject(ApiService);

  getOverview(): Observable<ParkOverview> {
    return this.api.get<ParkOverview>('/parkplatz/overview');
  }

  getHistorie(limit = 50): Observable<ParkHistorieEintrag[]> {
    return this.api.get<ParkHistorieEintrag[]>(`/parkplatz/historie?limit=${limit}`);
  }

  checkin(dto: ParkCheckinRequest): Observable<ParkClaim> {
    return this.api.post<ParkClaim>('/parkplatz/checkin', dto);
  }

  updateClaim(id: string, voraussichtlichBis: string | null): Observable<ParkClaim> {
    return this.api.put<ParkClaim>(`/parkplatz/claims/${id}`, { voraussichtlichBis });
  }

  freigeben(claimId: string): Observable<void> {
    return this.api.post<void>(`/parkplatz/claims/${claimId}/freigeben`, {});
  }

  problemMelden(accountId: string): Observable<void> {
    return this.api.post<void>(`/parkplatz/accounts/${accountId}/problem`, {});
  }

  updateAccount(id: string, dto: ParkAccountUpdateRequest): Observable<ParkAccountStatus> {
    return this.api.put<ParkAccountStatus>(`/parkplatz/accounts/${id}`, dto);
  }
}
