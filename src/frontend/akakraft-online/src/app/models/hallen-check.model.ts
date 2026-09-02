export interface HallenCheck {
  id: string;
  userId: string;
  displayName: string;
  pictureUrl?: string | null;
  message?: string | null;
  checkedInAt: string;
  expiresAt: string;
}
