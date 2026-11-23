export type FahrzeugArt = 'Pkw' | 'Lkw' | 'Motorrad' | 'Anhaenger' | 'Fahrrad';

/** `icon` = Material-Ligatur, `svgIcon` = eigenes Icon aus core/icons/custom-icons.ts. */
export const FAHRZEUG_ARTEN: { value: FahrzeugArt; label: string; icon?: string; svgIcon?: string }[] = [
  { value: 'Pkw', label: 'PKW', icon: 'directions_car' },
  { value: 'Lkw', label: 'LKW', icon: 'local_shipping' },
  { value: 'Motorrad', label: 'Motorrad', icon: 'two_wheeler' },
  { value: 'Anhaenger', label: 'Anhänger', svgIcon: 'trailer' },
  { value: 'Fahrrad', label: 'Fahrrad', icon: 'pedal_bike' },
];

/** Fahrräder brauchen kein Kennzeichen, alle anderen Arten schon. */
export function kennzeichenPflicht(art: FahrzeugArt | null | undefined): boolean {
  return art !== 'Fahrrad';
}

export function fahrzeugArtInfo(art: FahrzeugArt | null | undefined) {
  return FAHRZEUG_ARTEN.find(a => a.value === art) ?? FAHRZEUG_ARTEN[0];
}

export interface Fahrzeug {
  id: string;
  marke: string;
  modell: string | null;
  kennzeichen: string;
  istStandard: boolean;
  art: FahrzeugArt;
}

export interface SaveFahrzeugRequest {
  marke: string;
  modell: string | null;
  /** Bei Fahrrad leer erlaubt. */
  kennzeichen: string;
  istStandard: boolean;
  art: FahrzeugArt;
}
