import { Injectable } from '@angular/core';
import { GeoPoint } from '../models/cluster.model';

@Injectable({
  providedIn: 'root'
})
export class LocationService {

  constructor() { }

  public pixelCoordinates(geoPoint: GeoPoint): number[] {
    const refX = 46.7712;
    const refY = 23.6236;

    const xOffset = geoPoint.Longitude - refX;
    const yOffset = geoPoint.Latitude - refY;

    const x = xOffset * 111321 / 350 * 1400;
    const y = yOffset * 111321 / 250 * 1250;

    const xMultiplier = 1.145;
    const yMultiplier = 1.148;

    return [x * xMultiplier - 5, y * yMultiplier - 5];
  }
}
