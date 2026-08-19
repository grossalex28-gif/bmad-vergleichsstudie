import { Preiskategorie } from './preiskategorie';

export interface VeranstaltungDetail {
  id: string;
  titel: string;
  beschreibung: string;
  dauerMinuten: number;
  altersfreigabe: number;
  spielstaetteName: string;
  raumName: string;
  zeitpunkt: string;
  preiskategorien: Preiskategorie[];
}
