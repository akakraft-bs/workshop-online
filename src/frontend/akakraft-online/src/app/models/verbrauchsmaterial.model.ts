export interface Verbrauchsmaterial {
  id: string;
  name: string;
  description: string;
  category: string;
  unit: string;
  quantity: number;
  minQuantity?: number;
  imageUrl?: string;
  thumbnailUrl?: string;
  storageLocation?: string;
  createdAt: string;
  isNachbestellt: boolean;
  nachbestelltVonName?: string;
  nachbestelltAt?: string;
}
