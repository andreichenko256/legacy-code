namespace TaxiDispatch;

public class Program
{
    static DispatchStore store = new DispatchStore();
    static Dispatcher disp = new Dispatcher(store);

    public static void Main(string[] args)
    {
        Console.WriteLine("=== Taxi Shvydko - dispatch console ===");
        Console.WriteLine("drivers on shift: " + store.Drivers.Count
            + ", surge zones: " + DispatchStore.ZoneSurge.Count);
        Console.WriteLine("-- new orders");
        Created(disp.CreateRide("Olena Kovalenko", "067 321-45-67", 1.5, 1.5, 6.0, 4.5,
            "eco", false, "center"));
        Created(disp.CreateRide("Maksym Rudenko", "(050) 765-43-21",
            6.5, 6.5, 12.0, 9.0, "BUSINESS", false, "center"));
        Created(disp.CreateRide("Iryna Melnyk", "093 111 22 44", 4.5, 4.0, 15.0, 12.0,
            "ECO", true, "airport"));
        Created(disp.CreateRide("Taras Hnatiuk", "+380 67 555 00 11", 7.5, 1.0, 9.0, 3.0,
            "ECO", false, "Station"));
        Created(disp.CreateRide("Oksana Rudenko", "068 222 33 44",
            11.0, 12.0, 16.0, 15.0, "ECO", false, "suburb"));
        Created(disp.CreateRide("  ", "0500000000", 1, 1, 2, 2, "ECO", false, null));
        Created(disp.CreateRide("Petro Bondar", "12-34", 1, 1, 2, 2, "ECO", false, null));
        Console.WriteLine("-- dispatch");
        TryAssign(1, 3, false);
        TryAssign(2, 3, false);
        TryAssign(3, 3, false);
        TryAssign(4, 3, true);
        TryAssign(5, 3, false);
        Move(5, RideStatus.Completed);
        TryAssign(5, 10, false);
        TryAssign(99, 3, false);
        Statuses();
        ReportService rep = new ReportService(store);
        Console.WriteLine("-- rides");
        rep.PrintRides();
        Console.WriteLine("-- fleet");
        rep.PrintFleet();
    }

    static void Created(int id)
    {
        Ride r = store.FindRide(id);
        if (r == null)
            Console.WriteLine("  error: " + disp.LastNote);
        else
            Console.WriteLine("  order #" + id + " " + r.Passenger + ", " + r.CarClass
                + (r.ChildSeat ? " + child seat" : "") + ", zone " + r.Zone);
    }

    static void TryAssign(int rideId, double km, bool upgrade)
    {
        int res = disp.Assign(rideId, km, upgrade);
        Ride r = store.FindRide(rideId);
        Driver d = store.FindDriver(res);
        if (d == null)
            Console.WriteLine("  #" + rideId + " (" + km + " km): " + res + ", " + disp.LastNote);
        else
            Console.WriteLine("  #" + rideId + " (" + km + " km) -> " + d.Name + ", "
                + r.PickupKm + " km away, surge " + r.Surge);
    }

    static void Statuses()
    {
        Console.WriteLine("-- status changes");
        Move(1, RideStatus.InProgress);
        Move(1, RideStatus.Completed);
        Move(2, RideStatus.Cancelled);
        TryAssign(2, 3, false);
        Move(3, RideStatus.Completed);
        Move(3, RideStatus.InProgress);
        Move(3, RideStatus.Completed);
        Move(4, RideStatus.InProgress);
        Move(4, RideStatus.Completed);
        Move(5, RideStatus.InProgress);
        Move(77, RideStatus.Cancelled);
    }

    static void Move(int rideId, RideStatus to)
    {
        bool ok = disp.ChangeStatus(rideId, to);
        Ride r = store.FindRide(rideId);
        string extra = "";
        if (ok && r.Fare > 0)
            extra = ", " + r.Fare + " UAH";
        Console.WriteLine("  #" + rideId + " -> " + to + ": " + (ok ? "ok" : "refused") + extra);
    }
}
