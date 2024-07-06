import { Component, ElementRef, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { Observable, Subscription, interval, switchMap } from 'rxjs';
import { LocationService } from './services/location.service';
import { GeoPoint } from './models/cluster.model';
import { VehicleDataService } from './services/vehicle-data.service';
import { DataPoint, VehicleData } from './models/vehicle-data.model';
import { ClusteringService } from './services/clustering.service';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css']
})
export class AppComponent implements OnInit, OnDestroy {
  title = 'traffic-viewer';

  @ViewChild('imageRef')
  imageRef!: ElementRef<HTMLImageElement>;
  @ViewChild('canvasRef')
  canvasRef!: ElementRef<HTMLCanvasElement>;

  public averageSpeed = 0;
  public averageSpeedKmph = 0;
  public averageAcceleration = 0;
  public averageTimeSpentInTrafficMin = 0;
  public averageTimeSpentInTrafficSec = 0;
  public averageTimeSpentInCongestionMin = 0;
  public averageTimeSpentInCongestionSec = 0;
  private samplesCount = 0;

  private subscription: Subscription = new Subscription;
  private subscriptionMetrics: Subscription = new Subscription;
  private currentVehicleData: VehicleData[] = [];
  private clusters: Map<number, Map<DataPoint, VehicleData[]>> = new Map();
  private colorCodes = ["#FF0000", "#0000FF", "#00FF00", "#FFFF00", "#FF00FF", "#00FFFF", "#FFA500", "#800080", "#A52A2A", "#FFC0CB" ];
  private congestedColorCodes = ["#222222", "#AAAAAA", "#333333", "#999999", "#444444", "#888888", "#555555", "#777777", "#666666" ];
  private vehicleMap = new Map<number, { firstTimestamp: Date, lastTimestamp: Date }>();

  constructor(private vehicleDataService: VehicleDataService, private locationService: LocationService, private clusterService: ClusteringService) {}

  ngOnInit() {
    this.subscription = interval(750).pipe(
      switchMap(() => this.vehicleDataService.getVehicleData())
    ).subscribe(data => {
      // remove stopped vehicles
      var i = 0;
      while (i < this.currentVehicleData.length) {
        const id = this.currentVehicleData[i].DataPoint.Id;
        if (data.findIndex(x => x.DataPoint.Id === id) === -1) {
          this.currentVehicleData.splice(i, 1);

          const vehicle = this.vehicleMap.get(id)!;
          vehicle.lastTimestamp = new Date();
          this.vehicleMap.set(id, vehicle);
        } else {
          i++;
        }
      }

      // compute simulation round average speed, acceleration
      if (data.length > 0) {
        this.averageSpeed = Number(((this.averageSpeed * this.samplesCount + data.reduce((sum, current) => sum + current.DataPoint.Speed, 0))
         / (this.samplesCount + data.length)).toFixed(2));
        this.averageAcceleration = Number(((this.averageAcceleration * this.samplesCount + data.reduce((sum, current) => sum + current.DataPoint.Acceleration, 0))
         / (this.samplesCount+ data.length)).toFixed(2));
        this.averageSpeedKmph = Number((3.6 * this.averageSpeed).toFixed(2));
         this.samplesCount += data.length;
      }

      // add to all vehicles data, update current data to be displayed
      data.forEach(d => {
        var index = this.currentVehicleData.findIndex(x => x.DataPoint.Id === d.DataPoint.Id);
        if (index > -1) {
          this.currentVehicleData[index] = d;
        } else {
          this.currentVehicleData.push(d);
          if (!this.vehicleMap.has(d.DataPoint.Id)) {
            const timestemp = new Date();
            this.vehicleMap.set(d.DataPoint.Id, { firstTimestamp: timestemp, lastTimestamp: timestemp });
          }
        }
      });

      // color the clusters
      const roadIdMap = this.clusterService.groupByRoadId(this.currentVehicleData);
      roadIdMap.forEach((dataPoints, roadId) => {
        const clusters = this.clusterService.fit(dataPoints, Math.max(this.currentVehicleData.length/10 + 1, 5), 3);
        this.clusters.set(roadId, clusters);
      });

      this.onImageLoad();
      },
      error => {
        console.error('Error fetching clusters', error);
      }
    );

    const metrics = interval(750);
    this.subscriptionMetrics = metrics.subscribe(() => {
      this.computeMetrics();
    });
  }

  ngOnDestroy() {
    if (this.subscription) {
      this.subscription.unsubscribe();
    }
    if (this.subscriptionMetrics) {
      this.subscriptionMetrics.unsubscribe();
    }
  }

  public onImageLoad() {
    const imageElement = this.imageRef.nativeElement;
    const canvasElement = this.canvasRef.nativeElement;
    const context = canvasElement.getContext('2d');

    // Set canvas dimensions to match the image
    canvasElement.width = imageElement.width;
    canvasElement.height = imageElement.height;

    // Set the canvas drawing context
    if (context) {
      context.clearRect(0, 0, canvasElement.width, canvasElement.height);
      context.lineWidth = 2;

      var index = 0;
      var congestedIndex = 0;

      this.clusters.forEach((c, i) => {
        c.forEach((vehiclesData, centroid) => {
          const clusterAverageSpeed = vehiclesData.reduce((sum, current) => sum + current.DataPoint.Speed, 0) / vehiclesData.length;
          const color = clusterAverageSpeed < 10 && vehiclesData.length > 10 ? this.congestedColorCodes[congestedIndex++] : this.colorCodes[index++];

          vehiclesData.forEach(vehicleData => {
            const location = this.locationService.pixelCoordinates(new GeoPoint(vehicleData.DataPoint.Y, vehicleData.DataPoint.X));

            this.drawBorder(location[0], location[1], 10, 10, context, 2);

            // Draw rectangles
            context.strokeStyle = 'black';
            context.fillStyle = color;

            context.fillRect(location[0], location[1], 10, 10);
          });

          if (index >= this.colorCodes.length) {
            index = 0;
          }
          if (congestedIndex >= this.congestedColorCodes.length) {
            congestedIndex = 0;
          }
        });
      });
    }
  }

  private computeMetrics(): void {
    // compute average time spent in traffic
    const timeSpentInTraffic = this.computeAverageTimeSpentInTraffic();
    this.averageTimeSpentInTrafficSec = Number((timeSpentInTraffic % 60).toFixed(0));
    this.averageTimeSpentInTrafficMin = Number(Math.floor(timeSpentInTraffic / 60));
  }

  generateRandomColor(): string {
    const r = Math.floor(Math.random() * 256).toString(16).padStart(2, '0');
    const g = Math.floor(Math.random() * 256).toString(16).padStart(2, '0');
    const b = Math.floor(Math.random() * 256).toString(16).padStart(2, '0');

    return `#${r}${g}${b}`;
  }

  drawBorder(xPos: number, yPos: number, width: number, height: number, ctx: CanvasRenderingContext2D, thickness = 2): void{
    ctx.fillStyle='#000';
    ctx.fillRect(xPos - (thickness), yPos - (thickness), width + (thickness * 2), height + (thickness * 2));
  }

  computeAverageTimeSpentInTraffic(): number {
    let totalTimeSpent = 0;
    let count = 0;
    this.vehicleMap.forEach((timestamps, id) => {
      const timeDiff = timestamps.lastTimestamp.getTime() - timestamps.firstTimestamp.getTime();

      if (timeDiff > 0) {
        count++;
        totalTimeSpent += timeDiff * 1.0 / 1000;
      }
    });

    // Compute average time spent
    return count > 0 ? 1.0 * (totalTimeSpent / count - 10) : 0;
  }
}
