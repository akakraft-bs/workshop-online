export type AufgabeStatus = 'Neu' | 'Zugewiesen' | 'Erledigt';

export interface Aufgabe {
  id: string;
  titel: string;
  beschreibung: string;
  fotoUrl?: string | null;
  status: AufgabeStatus;
  assignedUserId?: string | null;
  assignedDisplayName?: string | null;
  assignedName?: string | null;
  createdByName: string;
  createdAt: string;
}

export interface AssignableUser {
  id: string;
  name: string;
}
