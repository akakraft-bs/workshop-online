export type GastschraubenArt = 'KastenPremiumbier' | 'ZwanzigEuroPayPal';

export interface HallenbuchEintrag {
  id: string;
  userId: string;
  userName: string;
  start: string;
  end: string;
  description: string;
  hatGastgeschraubt: boolean;
  gastschraubenArt: GastschraubenArt | null;
  gastschraubenBezahlt: boolean | null;
  hatFamiliegeschraubt: boolean;
  createdAt: string;
  fahrzeugId?: string | null;
  fahrzeugLabel?: string | null;
}

export interface CreateHallenbuchEintragDto {
  start: string;
  end: string;
  description: string;
  hatGastgeschraubt: boolean;
  gastschraubenArt: GastschraubenArt | null;
  gastschraubenBezahlt: boolean | null;
  hatFamiliegeschraubt: boolean;
  fahrzeugId?: string | null;
}

export interface UpdateHallenbuchEintragDto {
  start: string;
  end: string;
  description: string;
  hatGastgeschraubt: boolean;
  gastschraubenArt: GastschraubenArt | null;
  gastschraubenBezahlt: boolean | null;
  hatFamiliegeschraubt: boolean;
  fahrzeugId?: string | null;
}
