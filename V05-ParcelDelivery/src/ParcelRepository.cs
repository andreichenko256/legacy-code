namespace ParcelDelivery;

// Сховище посилок у пам'яті.
// TODO: перейти на базу даних (обіцяли ще у 2023)
public class ParcelRepository
{
    private static int nextId = 1;
    public List<Parcel> Items = new List<Parcel>();

    public string Add(Parcel p)
    {
        p.TrackingNo = "PD" + nextId.ToString("D6");
        nextId++;
        Items.Add(p);
        return p.TrackingNo;
    }

    // returns null when nothing found
    public Parcel FindByTracking(string trackingNo)
    {
        foreach (var p in Items)
            if (p.TrackingNo == trackingNo)
                return p;
        return null;
    }

    // onlyActive: true - маршрут кур'єра, false - звіт по зоні
    public List<Parcel> FindByZone(string zone, bool onlyActive)
    {
        List<Parcel> res = new List<Parcel>();
        foreach (var p in Items)
        {
            if (p.Zone != zone)
                continue;
            if (onlyActive && p.IsClosed())
                continue;
            res.Add(p);
        }
        return res;
    }

    public void PrintAll()
    {
        Console.WriteLine("All parcels: " + Items.Count);
        foreach (var p in Items)
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
}
