export type WunschStatus = 'Offen' | 'Angeschafft' | 'Abgelehnt';

export interface Wunsch {
  id: string;
  title: string;
  description: string;
  link?: string;
  status: WunschStatus;
  createdByUserId: string;
  createdByName: string;
  createdAt: string;
  upVotes: number;
  downVotes: number;
  currentUserVote?: boolean | null;  // true=up, false=down, null/undefined=none
  closedByUserId?: string;
  closedByName?: string;
  closedAt?: string;
  closeNote?: string;
}
