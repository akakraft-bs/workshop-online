export enum Role {
  None = 'None',
  Member = 'Member',
  Getraenkewart = 'Getraenkewart',
  Grillwart = 'Grillwart',
  Hallenwart = 'Hallenwart',
  Veranstaltungswart = 'Veranstaltungswart',
  Treasurer = 'Treasurer',
  ViceChairman = 'ViceChairman',
  Chairman = 'Chairman',
  Admin = 'Admin',
}

export const VORSTAND_ROLES: Role[] = [
  Role.Getraenkewart,
  Role.Grillwart,
  Role.Hallenwart,
  Role.Veranstaltungswart,
  Role.Treasurer,
  Role.ViceChairman,
  Role.Chairman,
];

export const ROLE_LABELS: Record<Role, string> = {
  [Role.None]: 'Kein Zugriff',
  [Role.Member]: 'Mitglied',
  [Role.Getraenkewart]: 'Getränkewart',
  [Role.Grillwart]: 'Grillwart',
  [Role.Hallenwart]: 'Hallenwart',
  [Role.Veranstaltungswart]: 'Veranstaltungswart',
  [Role.Treasurer]: 'Kassenwart',
  [Role.ViceChairman]: '2. Vorsitzender',
  [Role.Chairman]: '1. Vorsitzender',
  [Role.Admin]: 'Administrator',
};

export interface User {
  id: string;
  email: string;
  name: string;
  displayName?: string | null;
  pictureUrl?: string;
  createdAt: string;
  roles: Role[];
}
