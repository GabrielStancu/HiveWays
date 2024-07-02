import { Component, ElementRef, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { Subscription, interval, switchMap } from 'rxjs';
import { ClustersService } from './services/clusters.service';
import { ClusteringResult } from './models/cluster-results.model';
import { LocationService } from './services/location.service';
import { Cluster, GeoPoint } from './models/cluster.model';
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

  private subscription: Subscription = new Subscription;
  private vehicleData: VehicleData[] = [];
  private currentVehicleData: VehicleData[] = [];
  private clusters: Map<number, Map<DataPoint, DataPoint[]>> = new Map();
  private colorCodes = ["#FF0000", "#0000FF", "#00FF00", "#FFFF00", "#FF00FF", "#00FFFF", "#FFA500", "#800080", "#A52A2A", "#FFC0CB" ];

  constructor(private vehicleDataService: VehicleDataService, private locationService: LocationService, private clusterService: ClusteringService) {}

  ngOnInit() {
    this.subscription = interval(750).pipe(
      switchMap(() => this.vehicleDataService.getVehicleData())
    ).subscribe(data => {
      var i = 0;
      while (i < this.currentVehicleData.length) {
        if (data.findIndex(x => x.DataPoint.Id === this.currentVehicleData[i].DataPoint.Id) === -1) {
          this.currentVehicleData.splice(i, 1);
        } else {
          i++;
        }
      }

      data.forEach(d => {
        this.vehicleData.push(d);
        var index = this.currentVehicleData.findIndex(x => x.DataPoint.Id === d.DataPoint.Id);
        if (index > -1) {
          this.currentVehicleData[index] = d;
        } else {
          this.currentVehicleData.push(d);
        }
      });

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
  }

  ngOnDestroy() {
    if (this.subscription) {
      this.subscription.unsubscribe();
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

      this.clusters.forEach((c, i) => {
        c.forEach((dataPoints, centroid) => {
          const color = this.colorCodes[index++];

          dataPoints.forEach(dataPoint => {
            const location = this.locationService.pixelCoordinates(new GeoPoint(dataPoint.Y, dataPoint.X));
            this.drawBorder(location[0], location[1], 10, 10, context, 2);

            // Draw rectangles
            context.strokeStyle = 'black';
            context.fillStyle = color;

            context.fillRect(location[0], location[1], 10, 10);
          })
        })
      });

      // this.currentVehicleData.forEach(d => {
      //   const location = this.locationService.pixelCoordinates(new GeoPoint(d.DataPoint.Y, d.DataPoint.X));

      //   this.drawBorder(location[0], location[1], 10, 10, context, 2);
      //   const color = this.generateRandomColor();
      //   // Draw rectangles
      //   context.strokeStyle = 'black';
      //   context.fillStyle = color;

      //   context.fillRect(location[0], location[1], 10, 10);
      // });
    }
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
}
