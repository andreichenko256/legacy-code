namespace FleetMaintenance;

// Табло в диспетчерській (норми легкові для всіх)
public class Dashboard
{
    private FleetRepository repo;

    public Dashboard(FleetRepository repo)
    {
        this.repo = repo;
    }

    // TODO: брати інтервали з планувальника
    public bool IsDue(Vehicle v, string kind, int km, int days)
    {
        ServiceRecord last = repo.LastService(v.Number, kind);
        if (v.Mileage - last.Mileage >= km || (DateTime.Today - last.Date).Days >= days)
            return true;
        return false;
    }

    public void Print()
    {
        Console.WriteLine("Fleet board:");
        int due = 0;
        foreach (Vehicle v in repo.Vehicles)
        {
            if (v.Status == "SOLD")
                continue;
            string flags = "";
            if (IsDue(v, "OIL", 15000, 365))
                flags = flags + " OIL";
            if (IsDue(v, "BRAKES", 45000, 730))
                flags = flags + " BRAKES";
            if (IsDue(v, "TYRES", 60000, 1460))
                flags = flags + " TYRES";
            if (IsDue(v, "INSPECTION", 9999999, 365)) // лише дата
                flags = flags + " INSP";
            if (flags != "")
                due++;
            string t = v.Type == "TRUCK" ? "T" : v.Type == "VAN" ? "V" : "C";
            Console.WriteLine("  " + v.Number + " " + t + " " + v.Status.PadRight(9)
                + (v.Make + " " + v.Model).PadRight(20) + (v.Mileage + " km").PadLeft(10) + flags);
        }
        Console.WriteLine("Due for service: " + due);
    }
}
