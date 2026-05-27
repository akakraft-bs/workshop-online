export type FeedbackStatus = 'New' | 'Read' | 'Done';

export interface Feedback {
  id: string;
  userId: string;
  userName: string;
  text: string;
  pageUrl: string;
  appVersion?: string | null;
  status: FeedbackStatus;
  createdAt: string;
}
