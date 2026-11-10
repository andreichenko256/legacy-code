namespace TaxiDispatch;

// статуси зберігаються числом - порядок не міняти
public enum RideStatus { Created, Assigned, InProgress, Completed, Cancelled }

public class Driver
{
    public int Id;
    public string Name;
    public string Phone;
    public double X, Y;              // GPS, км від центру
    public string CarClass;          // ECO, COMFORT, BUSINESS, VAN
    public string Plate;
    public double Rating;
    public string Status = "free";   // free/onride/break/offline
    public int ActiveRides;
    public bool HasChildSeat;
    public string Zone;              // стоянка (для черги)
    public int TripsToday;
    public double EarnedToday;
}

public class Ride
{
    public int Id;
    public string Passenger;
    public string Phone;
    public double PickupX, PickupY;
    public double DropX, DropY;
    public string Zone;
    public string CarClass;
    public bool ChildSeat;
    public RideStatus Status = RideStatus.Created;
    public int DriverId;            // 0 - ще не призначено
    public double PickupKm;
    public double Surge = 1.0;
    public double Fare;             // вартість або штраф
    public DateTime AssignedAt;
    public DateTime StartedAt;
}
