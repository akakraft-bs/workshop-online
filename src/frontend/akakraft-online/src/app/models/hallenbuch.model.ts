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
  createdAt: string;
}

export interface CreateHallenbuchEintragDto {
  start: string;
  end: string;
  description: string;
  hatGastgeschraubt: boolean;
  gastschraubenArt: GastschraubenArt | null;
  gastschraubenBezahlt: boolean | null;
}

export interface UpdateHallenbuchEintragDto {
  start: string;
  end: string;
  description: string;
  hatGastgeschraubt: boolean;
  gastschraubenArt: GastschraubenArt | null;
  gastschraubenBezahlt: boolean | null;
}
