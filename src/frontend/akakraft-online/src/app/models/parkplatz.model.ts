export type ParkBerechtigungArt = 'Automatisch' | 'Selbstbestaetigt' | 'Spontan';

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
  zugangKonfiguriert: boolean;
  kennzeichen: string[];
  kennzeichenFehler: string | null;
}

export interface ParkAccountAdmin {
  id: string;
  label: string;
  portalUrl: string | null;
  notiz: string | null;
  portalUsername: string | null;
  zugangKonfiguriert: boolean;
  sortOrder: number;
}

export interface ParkKennzeichenListe {
  accountId: string;
  accountLabel: string;
  zugangKonfiguriert: boolean;
  max: number;
  kennzeichen: string[];
  fehler: string | null;
}

export type ParkKennzeichenAktion = 'Hinzugefuegt' | 'Entfernt';

export interface ParkKennzeichenAudit {
  id: string;
  ausgefuehrtVon: string;
  aktion: ParkKennzeichenAktion;
  kennzeichen: string;
  kennzeichenNachher: string[];
  createdAt: string;
}

export interface ParkZugangStatus {
  accountId: string;
  zugangKonfiguriert: boolean;
  username: string | null;
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

export type ParkHistorieTyp = 'Belegung' | 'KennzeichenHinzugefuegt' | 'KennzeichenEntfernt';

export interface ParkHistorieEintrag {
  id: string;
  typ: ParkHistorieTyp;
  zeitpunkt: string;
  accountLabel: string;
  displayName: string;
  kennzeichen: string | null;
  fahrzeugBezeichnung: string | null;
  einfahrtAt: string | null;
  freigegebenAt: string | null;
  autoExpiresAt: string | null;
  berechtigungArt: ParkBerechtigungArt | null;
  bestaetigungHinweis: string | null;
  status: ParkHistorieStatus | null;
}
