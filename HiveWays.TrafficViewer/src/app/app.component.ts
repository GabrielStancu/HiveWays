import { Component, ElementRef, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { Subscription, interval, switchMap } from 'rxjs';
import { ClustersService } from './services/clusters.service';
import { ClusteringResult } from './models/cluster-results.model';
import { LocationService } from './services/location.service';
import { Cluster, GeoPoint } from './models/cluster.model';
import { VehicleDataService } from './services/vehicle-data.service';
import { VehicleData } from './models/vehicle-data.model';

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

  constructor(private vehicleDataService: VehicleDataService, private locationService: LocationService) {}

  ngOnInit() {
    this.subscription = interval(5000).pipe(
      switchMap(() => this.vehicleDataService.getVehicleData())
    ).subscribe(data => {
      console.log(data);
      data.forEach(d => {
        this.vehicleData.push(d);
        var index = this.currentVehicleData.findIndex(x => x.DataPoint.Id === d.DataPoint.Id);
        if (index > -1) {
          this.currentVehicleData[index] = d;
        } else {
          this.currentVehicleData.push(d);
        }
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

      this.currentVehicleData.forEach(d => {
        const location = this.locationService.pixelCoordinates(new GeoPoint(d.DataPoint.Y, d.DataPoint.X));

        this.drawBorder(location[0], location[1], 10, 10, context, 2);
        const color = this.generateRandomColor();
        // Draw rectangles
        context.strokeStyle = 'black';
        context.fillStyle = color;

        context.fillRect(location[0], location[1], 10, 10);
      });
    }
  }

  generateRandomColor(): string {
    // Generate random values for R, G, B channels
    // const r = Math.floor(Math.random() * 256).toString(16).padStart(2, '0');
    // const g = Math.floor(Math.random() * 256).toString(16).padStart(2, '0');
    // const b = Math.floor(Math.random() * 256).toString(16).padStart(2, '0');

    // return `#${r}${g}${b}`;
    return 'orange';
  }

  drawBorder(xPos: number, yPos: number, width: number, height: number, ctx: CanvasRenderingContext2D, thickness = 2): void{
    ctx.fillStyle='#000';
    ctx.fillRect(xPos - (thickness), yPos - (thickness), width + (thickness * 2), height + (thickness * 2));
  }
}
