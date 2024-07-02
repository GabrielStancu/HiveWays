import { Injectable } from '@angular/core';
import { DataPoint, VehicleData } from '../models/vehicle-data.model';

@Injectable({
  providedIn: 'root'
})
export class ClusteringService {

  private k = 0;
  private maxIterations = 0;

  constructor() { }

  public groupByRoadId(vehicleDataList: VehicleData[]): Map<number, DataPoint[]> {
    const roadIdMap = new Map<number, DataPoint[]>();

    vehicleDataList.forEach(vehicleData => {
      const roadId = vehicleData.RoadId;
      if (!roadIdMap.has(roadId)) {
        roadIdMap.set(roadId, []);
      }
      roadIdMap.get(roadId)?.push(vehicleData.DataPoint);
    });

    return roadIdMap;
  }

  public fit(data: DataPoint[], k: number, maxIterations: number): Map<DataPoint, DataPoint[]> {
    this.k = k;
    this.maxIterations = maxIterations;

    let centroids = this.initializeCentroids(data);
    let clusters = new Map<DataPoint, DataPoint[]>();

    for (let i = 0; i < this.maxIterations; i++) {
      clusters = this.assignClusters(data, centroids);
      const newCentroids = this.calculateCentroids(clusters);

      if (JSON.stringify(centroids) === JSON.stringify(newCentroids)) {
        break;
      }

      centroids = newCentroids;
    }

    return clusters;
  }

  private initializeCentroids(data: DataPoint[]): DataPoint[] {
    const shuffled = data.sort(() => 0.5 - Math.random());
    return shuffled.slice(0, this.k);
  }

  private getDistance(point1: DataPoint, point2: DataPoint): number {
    return Math.sqrt(Math.pow(point1.X - point2.X, 2) + Math.pow(point1.Y - point2.Y, 2));
  }

  private assignClusters(data: DataPoint[], centroids: DataPoint[]): Map<DataPoint, DataPoint[]> {
    const clusters = new Map<DataPoint, DataPoint[]>();

    centroids.forEach(centroid => clusters.set(centroid, []));

    data.forEach(point => {
      let closestCentroid = centroids[0];
      let minDistance = this.getDistance(point, closestCentroid);

      for (let i = 1; i < centroids.length; i++) {
        const distance = this.getDistance(point, centroids[i]);
        if (distance < minDistance) {
          minDistance = distance;
          closestCentroid = centroids[i];
        }
      }

      clusters.get(closestCentroid)?.push(point);
    });

    return clusters;
  }

  private calculateCentroids(clusters: Map<DataPoint, DataPoint[]>): DataPoint[] {
    const newCentroids: DataPoint[] = [];

    clusters.forEach((points, centroid) => {
      if (points.length === 0) {
        newCentroids.push(centroid);
        return;
      }

      const sum = points.reduce((acc, point) => {
        acc.X += point.X;
        acc.Y += point.Y;
        return acc;
      }, { X: 0, Y: 0 });

      newCentroids.push({
        ...centroid,
        X: sum.X / points.length,
        Y: sum.Y / points.length,
      });
    });

    return newCentroids;
  }
}


