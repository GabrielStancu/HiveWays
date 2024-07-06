import { Injectable } from '@angular/core';
import { DataPoint, VehicleData } from '../models/vehicle-data.model';

@Injectable({
  providedIn: 'root'
})
export class ClusteringService {

  private k = 0;
  private maxIterations = 0;

  constructor() { }

  public groupByRoadId(vehicleDataList: VehicleData[]): Map<number, VehicleData[]> {
    const roadIdMap = new Map<number, VehicleData[]>();

    vehicleDataList.forEach(vehicleData => {
      const roadId = vehicleData.RoadId;
      if (!roadIdMap.has(roadId)) {
        roadIdMap.set(roadId, []);
      }
      roadIdMap.get(roadId)?.push(vehicleData);
    });

    return roadIdMap;
  }

  public fit(data: VehicleData[], k: number, maxIterations: number): Map<DataPoint, VehicleData[]> {
    this.k = k;
    this.maxIterations = maxIterations;

    let centroids = this.initializeCentroids(data.map(d => d.DataPoint));
    let clusters = new Map<DataPoint, VehicleData[]>();

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

  private assignClusters(data: VehicleData[], centroids: DataPoint[]): Map<DataPoint, VehicleData[]> {
    const clusters = new Map<DataPoint, VehicleData[]>();

    centroids.forEach(centroid => clusters.set(centroid, []));

    data.forEach(point => {
      let closestCentroid = centroids[0];
      let minDistance = this.getDistance(point.DataPoint, closestCentroid);

      for (let i = 1; i < centroids.length; i++) {
        const distance = this.getDistance(point.DataPoint, centroids[i]);
        if (distance < minDistance) {
          minDistance = distance;
          closestCentroid = centroids[i];
        }
      }

      clusters.get(closestCentroid)?.push(point);
    });

    return clusters;
  }

  private calculateCentroids(clusters: Map<DataPoint, VehicleData[]>): DataPoint[] {
    const newCentroids: DataPoint[] = [];

    clusters.forEach((points, centroid) => {
      if (points.length === 0) {
        newCentroids.push(centroid);
        return;
      }

      const sum = points.reduce((acc, point) => {
        acc.X += point.DataPoint.X;
        acc.Y += point.DataPoint.Y;
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


