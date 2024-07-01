import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { VehicleData } from '../models/vehicle-data.model';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class VehicleDataService {
  private apiUrl = 'http://localhost:7115/api/VehicleDataReader';

  constructor(private http: HttpClient) {}

  public getVehicleData(): Observable<VehicleData[]> {
    return this.http.get<VehicleData[]>(this.apiUrl);
  }
}
