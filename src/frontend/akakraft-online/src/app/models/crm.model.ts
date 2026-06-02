export type PartnerStatus = 'Potentiell' | 'Angeschrieben' | 'InVerhandlung' | 'Aktiv' | 'Abgelehnt' | 'Inaktiv';
export type KontaktKanal = 'Email' | 'Telefon' | 'Meeting' | 'Brief' | 'Sonstiges';
export type KontaktReaktion = 'Positiv' | 'Neutral' | 'Negativ' | 'KeineAntwort';

export interface PartnerOverview {
  id: string;
  name: string;
  kategorie: string | null;
  status: PartnerStatus;
  website: string | null;
  anzahlKontakte: number;
  letzterKontakt: string | null;
}

export interface AnsprechpartnerDto {
  id: string;
  name: string;
  position: string | null;
  email: string | null;
  telefon: string | null;
  notizen: string | null;
}

export interface KontakteintragDto {
  id: string;
  ansprechpartnerId: string | null;
  ansprechpartnerName: string | null;
  datum: string;
  kanal: KontaktKanal;
  reaktion: KontaktReaktion;
  zusammenfassung: string;
  naechsteSchritte: string | null;
  erstelltAm: string;
}

export interface PartnerDetail {
  id: string;
  name: string;
  kategorie: string | null;
  status: PartnerStatus;
  website: string | null;
  notizen: string | null;
  ansprechpartner: AnsprechpartnerDto[];
  kontakteintraege: KontakteintragDto[];
}

export const PARTNER_STATUS_LABELS: Record<PartnerStatus, string> = {
  Potentiell: 'Potentiell',
  Angeschrieben: 'Angeschrieben',
  InVerhandlung: 'In Verhandlung',
  Aktiv: 'Aktiv',
  Abgelehnt: 'Abgelehnt',
  Inaktiv: 'Inaktiv',
};

export const KANAL_LABELS: Record<KontaktKanal, string> = {
  Email: 'E-Mail',
  Telefon: 'Telefon',
  Meeting: 'Meeting',
  Brief: 'Brief',
  Sonstiges: 'Sonstiges',
};

export const KANAL_ICONS: Record<KontaktKanal, string> = {
  Email: 'email',
  Telefon: 'phone',
  Meeting: 'groups',
  Brief: 'mail',
  Sonstiges: 'contact_support',
};

export const REAKTION_LABELS: Record<KontaktReaktion, string> = {
  Positiv: 'Positiv',
  Neutral: 'Neutral',
  Negativ: 'Negativ',
  KeineAntwort: 'Keine Antwort',
};
