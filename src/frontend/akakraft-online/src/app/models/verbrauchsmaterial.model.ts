export interface Verbrauchsmaterial {
  id: string;
  name: string;
  description: string;
  category: string;
  unit: string;
  quantity: number;
  minQuantity?: number;
  imageUrl?: string;
}
