import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../api/api.service';
import {
  ParkAccountAdmin,
  ParkAccountStatus,
  ParkAccountUpdateRequest,
  ParkCheckinRequest,
  ParkClaim,
  ParkHistorieEintrag,
  ParkKennzeichenAudit,
  ParkKennzeichenListe,
  ParkOverview,
  ParkZugangStatus,
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

  getAdminAccounts(): Observable<ParkAccountAdmin[]> {
    return this.api.get<ParkAccountAdmin[]>('/parkplatz/accounts');
  }

  updateAccount(id: string, dto: ParkAccountUpdateRequest): Observable<ParkAccountStatus> {
    return this.api.put<ParkAccountStatus>(`/parkplatz/accounts/${id}`, dto);
  }

  setZugang(id: string, username: string | null, password: string | null): Observable<ParkZugangStatus> {
    return this.api.put<ParkZugangStatus>(`/parkplatz/accounts/${id}/zugang`, { username, password });
  }

  getKennzeichen(accountId: string): Observable<ParkKennzeichenListe> {
    return this.api.get<ParkKennzeichenListe>(`/parkplatz/accounts/${accountId}/kennzeichen`);
  }

  addKennzeichen(accountId: string, kennzeichen: string): Observable<ParkKennzeichenListe> {
    return this.api.post<ParkKennzeichenListe>(`/parkplatz/accounts/${accountId}/kennzeichen`, { kennzeichen });
  }

  removeKennzeichen(accountId: string, code: string): Observable<ParkKennzeichenListe> {
    return this.api.delete<ParkKennzeichenListe>(
      `/parkplatz/accounts/${accountId}/kennzeichen?code=${encodeURIComponent(code)}`);
  }

  getKennzeichenHistorie(accountId: string): Observable<ParkKennzeichenAudit[]> {
    return this.api.get<ParkKennzeichenAudit[]>(`/parkplatz/accounts/${accountId}/kennzeichen/historie`);
  }
}
