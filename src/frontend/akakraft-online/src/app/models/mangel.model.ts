export type MangelKategorie = 'Halle' | 'Werkzeug' | 'Sonstiges';
export type MangelStatus = 'Offen' | 'Kenntnisgenommen' | 'Behoben' | 'Abgelehnt' | 'Zurueckgezogen';

export interface MangelAnmerkung {
  id: string;
  text: string;
  createdByUserId: string;
  createdByName: string;
  createdAt: string;
  updatedAt?: string | null;
}

export interface Mangel {
  id: string;
  title: string;
  description: string;
  kategorie: MangelKategorie;
  status: MangelStatus;
  createdByUserId: string;
  createdByName: string;
  createdAt: string;
  imageUrl?: string;
  resolvedByUserId?: string;
  resolvedByName?: string;
  resolvedAt?: string;
  note?: string;
  anmerkungen: MangelAnmerkung[];
}

export const MANGEL_KATEGORIE_LABELS: Record<MangelKategorie, string> = {
  Halle: 'Halle',
  Werkzeug: 'Werkzeug',
  Sonstiges: 'Sonstiges',
};

export const MANGEL_STATUS_LABELS: Record<MangelStatus, string> = {
  Offen: 'Offen',
  Kenntnisgenommen: 'Kenntnisgenommen',
  Behoben: 'Behoben',
  Abgelehnt: 'Abgelehnt',
  Zurueckgezogen: 'Zurückgezogen',
};
