export interface Werkzeug {
  id: string;
  name: string;
  description: string;
  category: string;
  imageUrl?: string;
  dimensions?: string;
  isAvailable: boolean;
  borrowedByUserId?: string;
  borrowedByName?: string;
  borrowedAt?: string;
  expectedReturnAt?: string;
  returnedAt?: string;
}
