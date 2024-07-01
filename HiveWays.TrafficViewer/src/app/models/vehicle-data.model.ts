export class VehicleData {
  public constructor(public Id: string, public Timestamp: Date, public RoadId: number, public DataPoint: DataPoint) {}
}

export class DataPoint {
  public constructor(public TimeOffsetSeconds: number, public Id: number, public X: number, public Y: number,
    public Speed: number, public Acceleration: number, public Heading : number) {}
}
