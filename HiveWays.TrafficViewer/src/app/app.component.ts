import { Component, ElementRef, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { Subscription, interval, switchMap } from 'rxjs';
import { ClustersService } from './services/clusters.service';
import { ClusteringResult } from './models/cluster-results.model';
import { LocationService } from './services/location.service';
import { Cluster, GeoPoint } from './models/cluster.model';

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
  clusters: Cluster[] = [];
  congestedClusters: Cluster[] = [];

  constructor(private clustersService: ClustersService, private locationService: LocationService) {}

  ngOnInit() {
    this.subscription = interval(1000).pipe(
      switchMap(() => this.clustersService.getClusteringResults())
    ).subscribe(data => {
      this.clusters = data.Clusters;
        this.congestedClusters = data.CongestedClusters;
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

      this.clusters.forEach(c => {
        const color = this.generateRandomColor();

        // Draw rectangles
        context.strokeStyle = color;
        context.fillStyle = color;

        c.Vehicles.forEach(v => {
          const longitude = v.Trajectory[0].Location.Longitude;
          const latitude = v.Trajectory[0].Location.Latitude;
          const location = this.locationService.pixelCoordinates(new GeoPoint(latitude, longitude));

          context.fillRect(location[0], location[1], 10, 10);
        })

      });
    }
  }

  generateRandomColor(): string {
    // Generate random values for R, G, B channels
    const r = Math.floor(Math.random() * 256).toString(16).padStart(2, '0');
    const g = Math.floor(Math.random() * 256).toString(16).padStart(2, '0');
    const b = Math.floor(Math.random() * 256).toString(16).padStart(2, '0');

    return `#${r}${g}${b}`;
  }
}
