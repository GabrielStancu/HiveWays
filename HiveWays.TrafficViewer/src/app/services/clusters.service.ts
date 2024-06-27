import { Injectable } from '@angular/core';
import { ClusteringResult } from '../models/cluster-results.model';
import { Observable } from 'rxjs';
import { HttpClient } from '@angular/common/http';

@Injectable({
  providedIn: 'root'
})
export class ClustersService {

  private apiUrl = 'http://localhost:7015/api/ClustersReader';

  constructor(private http: HttpClient) {}

  getClusteringResults(): Observable<ClusteringResult> {
    return this.http.get<ClusteringResult>(this.apiUrl);
  }
}
