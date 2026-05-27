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
  description?: string | null;
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
  linkedEventId?: string | null;
  linkedCalendarId?: string | null;
  linkedEventTitle?: string | null;
  linkedEventStart?: string | null;
}

export interface CreateUmfrageDto {
  question: string;
  description?: string | null;
  options: string[];
  isMultipleChoice: boolean;
  resultsVisible: boolean;
  revealAfterClose: boolean;
  deadline?: string | null;
  linkedEventId?: string | null;
  linkedCalendarId?: string | null;
  linkedEventTitle?: string | null;
  linkedEventStart?: string | null;
}

export interface UpdateUmfrageOptionDto {
  id?: string | null;
  text: string;
}

export interface UpdateUmfrageDto {
  question: string;
  description?: string | null;
  options: UpdateUmfrageOptionDto[];
  isMultipleChoice: boolean;
  resultsVisible: boolean;
  revealAfterClose: boolean;
  deadline?: string | null;
  linkedEventId?: string | null;
  linkedCalendarId?: string | null;
  linkedEventTitle?: string | null;
  linkedEventStart?: string | null;
}

export interface VoteUmfrageDto {
  optionIds: string[];
}
