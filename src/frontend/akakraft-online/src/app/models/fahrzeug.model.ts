export interface Fahrzeug {
  id: string;
  marke: string;
  modell: string | null;
  kennzeichen: string;
  istStandard: boolean;
}

export interface SaveFahrzeugRequest {
  marke: string;
  modell: string | null;
  kennzeichen: string;
  istStandard: boolean;
}
