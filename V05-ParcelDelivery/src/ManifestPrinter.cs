namespace ParcelDelivery;

// Маршрутні листи для кур'єрів і звіт по зоні
public class ManifestPrinter
{
    private ParcelRepository repo;

    public ManifestPrinter(ParcelRepository repo) { this.repo = repo; }

    public void PrintManifest(string zone)
    {
        List<Parcel> list = repo.FindByZone(zone, true);
        Console.WriteLine("Manifest " + zone + " (" + pick_vehicle(list) + "): " + list.Count);
        foreach (var p in list)
        {
            string line = p.TrackingNo + " | " + p.Zone + " | " + p.Tariff;
            line = line + " | " + p.WeightKg.ToString("0.00") + " kg";
            line = line + " | " + p.Status;
            if (p.Price > 0)
                line = line + " | " + p.Price.ToString("0.00");
            else
                line = line + " | n/a";
            Console.WriteLine("  " + line);
        }
    }

    // OVERSIZE возимо тільки вантажівкою
    public string pick_vehicle(List<Parcel> list)
    {
        decimal kg = 0m;
        bool big = false;
        foreach (var p in list)
        {
            kg = kg + p.WeightKg * p.Pieces;
            if (p.Tariff == "OVERSIZE")
                big = true;
        }
        if (big || kg > 300m)
            return "truck";
        if (kg > 50m)
            return "van";
        return "car";
    }

    public void PrintZoneSummary(string zone)
    {
        var items = repo.FindByZone(zone, false);
        decimal total = 0m;
        int ret = 0;
        foreach (var p in items)
        {
            if (p.Status == ParcelStatus.Returned)
                ret++;
            else
                total = total + p.Price;
        }
        Console.WriteLine("Zone " + zone + " summary: " + (items.Count - ret) + " paid, " + ret
            + " returned, " + total.ToString("0.00") + " UAH");
    }
}
