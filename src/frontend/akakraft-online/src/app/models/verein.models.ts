export interface AmtsTraeger {
  role: string;
  roleLabel: string;
  userId: string;
  userName: string;
  phone?: string | null;
  address?: string | null;
}

export interface Schluesselhinterlegung {
  id: string;
  name: string;
  address: string;
  phone?: string | null;
  sortOrder: number;
}

export interface VereinInfo {
  amtstraeger: AmtsTraeger[];
  schluessel: Schluesselhinterlegung[];
}

export interface DokumentDto {
  id: string;
  folderId: string;
  fileName: string;
  fileUrl: string;
  uploadedByName: string;
  uploadedAt: string;
  fileSizeBytes?: number | null;
}

export interface DokumentOrdnerDto {
  id: string;
  name: string;
  createdAt: string;
  dokumente: DokumentDto[];
}

export interface VereinZugang {
  id: string;
  anbieter: string;
  zugangsdaten: string;
}

export type ProjektStatus = 'Geplant' | 'Gestartet' | 'Abgeschlossen';

export interface ProjektDto {
  id: string;
  name: string;
  description?: string | null;
  plannedStartDate: string;
  durationWeeks: number;
  actualStartDate?: string | null;
  actualEndDate?: string | null;
  expectedEndDate: string;
  status: ProjektStatus;
  projektplanUrl?: string | null;
  createdByName: string;
  createdAt: string;
}
