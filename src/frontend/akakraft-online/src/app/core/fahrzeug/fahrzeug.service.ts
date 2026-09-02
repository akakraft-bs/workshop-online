import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../api/api.service';
import { Fahrzeug, SaveFahrzeugRequest } from '../../models/fahrzeug.model';

@Injectable({ providedIn: 'root' })
export class FahrzeugService {
  private readonly api = inject(ApiService);

  list(): Observable<Fahrzeug[]> {
    return this.api.get<Fahrzeug[]>('/fahrzeuge');
  }

  create(dto: SaveFahrzeugRequest): Observable<Fahrzeug> {
    return this.api.post<Fahrzeug>('/fahrzeuge', dto);
  }

  update(id: string, dto: SaveFahrzeugRequest): Observable<Fahrzeug> {
    return this.api.put<Fahrzeug>(`/fahrzeuge/${id}`, dto);
  }

  remove(id: string): Observable<void> {
    return this.api.delete<void>(`/fahrzeuge/${id}`);
  }
}
