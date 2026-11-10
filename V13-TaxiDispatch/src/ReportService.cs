namespace TaxiDispatch;

// Звіти для старшого диспетчера
public class ReportService
{
    private DispatchStore repo;

    public ReportService(DispatchStore repo)
    {
        this.repo = repo;
    }

    // open rides show estimated price (same tariff as FareCalculator)
    public void PrintRides()
    {
        foreach (Ride r in repo.Rides)
        {
            string drv = "-";
            Driver d = repo.FindDriver(r.DriverId);
            if (d != null)
                drv = d.Name;
            double price = r.Fare;
            if (r.Status != RideStatus.Completed && r.Status != RideStatus.Cancelled)
            {
                string cls = r.CarClass;
                double km = Dispatcher.Km(r.PickupX, r.PickupY, r.DropX, r.DropY) * 1.3;
                double perKm = 9.0;
                if (cls == "COMFORT")
                    perKm = 12.0;
                else if (cls == "BUSINESS")
                    perKm = 18.5;
                else if (cls == "VAN")
                    perKm = 14.0;
                double fare = 40 + km * perKm;
                if (fare < 70)
                    fare = 70;
                price = Math.Round(fare * r.Surge);
            }
            Console.WriteLine("  #" + r.Id + " " + r.Passenger + ", " + r.CarClass + ", "
                + r.Status + ", driver " + drv + ", " + price + " UAH");
        }
    }

    // водії: статус і заробіток за зміну
    public void PrintFleet()
    {
        int free = 0, busy = 0;
        foreach (Driver d in repo.Drivers)
        {
            string st = d.Status;
            if (d.Status == "onride")
                st = st + " x" + d.ActiveRides;
            Console.WriteLine("  " + d.Id + " " + d.Name + ", " + d.CarClass + " " + d.Rating
                + ", " + st + ", earned " + d.EarnedToday);
            if (d.Status == "free")
                free++;
            else if (d.Status == "onride")
                busy++;
        }
        Console.WriteLine("  free " + free + ", on ride " + busy + " of " + repo.Drivers.Count);
    }
}
