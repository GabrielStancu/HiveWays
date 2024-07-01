import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { HttpClientModule } from '@angular/common/http';

import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';
import { ClustersService } from './services/clusters.service';
import { LocationService } from './services/location.service';
import { VehicleDataService } from './services/vehicle-data.service';

@NgModule({
  declarations: [
    AppComponent
  ],
  imports: [
    BrowserModule,
    AppRoutingModule,
    HttpClientModule
  ],
  providers: [ClustersService, LocationService, VehicleDataService],
  bootstrap: [AppComponent]
})
export class AppModule { }
