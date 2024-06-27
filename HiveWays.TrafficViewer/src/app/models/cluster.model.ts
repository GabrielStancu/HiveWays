export class Cluster {
  constructor(public Id: number,
    public AverageSpeed: number,
    public AverageAcceleration: number,
    public VehiclesCount: number,
    public Center: GeoPoint,
    public Vehicles: Vehicle[]
  ) {}
}

export class GeoPoint {
  constructor(public Latitude: number, public Longitude: number) {}
}

export class Vehicle {
  constructor(public Id: number,
    public Trajectory: VehicleLocation[]
  ) {}
}

export class VehicleLocation {
  constructor(public Timestamp: Date,
    public Location: GeoPoint,
    public SpeedKmph: number,
    public Heading: number,
    public AccelerationKmph: number,
    public RoadId: number
  ) {}
}
