export type UmfrageStatus = 'Offen' | 'Geschlossen';

export interface UmfrageOption {
  id: string;
  text: string;
  sortOrder: number;
  /** Null when results are hidden for the current user. */
  voteCount: number | null;
  /** Null when results are hidden for the current user. */
  voters: string[] | null;
}

export interface Umfrage {
  id: string;
  question: string;
  isMultipleChoice: boolean;
  resultsVisible: boolean;
  revealAfterClose: boolean;
  deadline?: string | null;
  status: UmfrageStatus;
  createdByUserId: string;
  createdByName: string;
  createdAt: string;
  closedByUserId?: string | null;
  closedByName?: string | null;
  closedAt?: string | null;
  options: UmfrageOption[];
  /** IDs of options the current user has selected. */
  currentUserOptionIds: string[];
  /** Total number of distinct users who answered. */
  participantCount: number;
}

export interface CreateUmfrageDto {
  question: string;
  options: string[];
  isMultipleChoice: boolean;
  resultsVisible: boolean;
  revealAfterClose: boolean;
  deadline?: string | null;
}

export interface UpdateUmfrageOptionDto {
  id?: string | null;
  text: string;
}

export interface UpdateUmfrageDto {
  question: string;
  options: UpdateUmfrageOptionDto[];
  isMultipleChoice: boolean;
  resultsVisible: boolean;
  revealAfterClose: boolean;
  deadline?: string | null;
}

export interface VoteUmfrageDto {
  optionIds: string[];
}
