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
}

export interface CreateHallenbuchEintragDto {
  start: string;
  end: string;
  description: string;
  hatGastgeschraubt: boolean;
  gastschraubenArt: GastschraubenArt | null;
  gastschraubenBezahlt: boolean | null;
  hatFamiliegeschraubt: boolean;
}

export interface UpdateHallenbuchEintragDto {
  start: string;
  end: string;
  description: string;
  hatGastgeschraubt: boolean;
  gastschraubenArt: GastschraubenArt | null;
  gastschraubenBezahlt: boolean | null;
  hatFamiliegeschraubt: boolean;
}
