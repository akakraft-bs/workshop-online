import { inject, Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { catchError, of } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class VersionService {
  private readonly http = inject(HttpClient);

  readonly version = signal('');

  constructor() {
    this.http.get<{ version: string }>('version.json').pipe(
      catchError(() => of({ version: '' })),
    ).subscribe(data => this.version.set(data.version));
  }
}
