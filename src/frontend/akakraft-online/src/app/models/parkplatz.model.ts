export type ParkBerechtigungArt = 'Automatisch' | 'Selbstbestaetigt' | 'Spontan';

/** Uni-Portal zur Kennzeichen-Verwaltung (Fallback, falls pro Konto keine URL hinterlegt ist). */
export const PARKPORTAL_URL = 'https://tu-braunschweig.campusparken.de/portal';

export interface ParkReservierung {
  eventId: string;
  titel: string;
  start: string | null;
  end: string | null;
  istMeine: boolean;
  fahrzeugId?: string | null;
  fahrzeugLabel?: string | null;
}

export interface ParkClaim {
  id: string;
  parkAccountId: string;
  userId: string;
  displayName: string;
  pictureUrl: string | null;
  kennzeichen: string;
  fahrzeugBezeichnung: string;
  einfahrtAt: string;
  voraussichtlichBis: string | null;
  autoExpiresAt: string;
  berechtigungArt: ParkBerechtigungArt;
}

export interface ParkAccountStatus {
  id: string;
  label: string;
  portalUrl: string | null;
  notiz: string | null;
  istFrei: boolean;
  belegung: ParkClaim | null;
}

export interface ParkBerechtigung {
  art: ParkBerechtigungArt;
  erfordertBestaetigung: boolean;
  hinweis: string;
  waehlbareReservierungen: ParkReservierung[];
  vorgeschlagenesFahrzeugId?: string | null;
  vorgeschlagenesFahrzeugLabel?: string | null;
}

export interface ParkOverview {
  accounts: ParkAccountStatus[];
  berechtigung: ParkBerechtigung;
  anstehendeReservierungen: ParkReservierung[];
  kalenderKonfiguriert: boolean;
}

export interface ParkCheckinRequest {
  parkAccountId: string;
  fahrzeugId: string | null;
  kennzeichen: string | null;
  fahrzeugBezeichnung: string | null;
  einfahrtAt: string | null;
  voraussichtlichBis: string | null;
  bestaetigungAkzeptiert: boolean;
  bookingEventId: string | null;
}

export interface ParkAccountUpdateRequest {
  label: string;
  portalUrl: string | null;
  notiz: string | null;
}

export type ParkHistorieStatus = 'Aktiv' | 'Freigegeben' | 'Abgelaufen';

export interface ParkHistorieEintrag {
  id: string;
  accountLabel: string;
  displayName: string;
  kennzeichen: string;
  fahrzeugBezeichnung: string;
  einfahrtAt: string;
  freigegebenAt: string | null;
  autoExpiresAt: string;
  berechtigungArt: ParkBerechtigungArt;
  bestaetigungHinweis: string | null;
  bookingEventId: string | null;
  status: ParkHistorieStatus;
}
