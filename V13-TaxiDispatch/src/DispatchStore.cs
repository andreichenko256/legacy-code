namespace TaxiDispatch;

// Дані зміни в пам'яті. TODO: перенести в БД
public class DispatchStore
{
    public static int nextRideId = 1;
    public static Dictionary<string, double> ZoneSurge = new Dictionary<string, double>();
    public List<Driver> Drivers = new List<Driver>();
    public List<Ride> Rides = new List<Ride>();

    static DispatchStore()
    {
        LoadZones("CENTER=100;AIRPORT=130;STATION=120;SUBURB=90;OLD_TOWN=1,15");
    }

    public DispatchStore()
    {
        add(1, "Ivan Petrenko", "ECO", "AA1024KI", 4.8, 2.0, 3.0, false);
        add(2, "Oleksii Moroz", "ECO", "AI7731BE", 4.0, 1.0, 1.0, false);
        add(3, "Natalia Kravets", "ECO", "AA5530OP", 4.6, 5.0, 5.0, true);
        add(4, "Dmytro Savchenko", "COMFORT", "KA2210AH", 4.9, 7.0, 2.0, false);
        add(5, "Yurii Bondarenko", "COMFORT", "AA0077XX", 4.5, 2.5, 2.5, true);
        add(6, "Andrii Melnyk", "BUSINESS", "AA0001MM", 4.7, 6.0, 6.0, false);
        add(7, "Serhii Tkachuk", "BUSINESS", "KA7777KA", 4.9, 8.0, 7.0, false);
        add(8, "Viktor Hnatiuk", "VAN", "AI4512KM", 4.4, 10.0, 10.0, true);
        add(9, "Roman Shevchenko", "ECO", "AA3319EB", 3.8, 4.0, 4.0, false);
        Drivers[4].Status = "break";
        Drivers[3].TripsToday = 12;     // з нічної зміни
    }

    private void add(int id, string name, string cls, string plate, double rating,
        double x, double y, bool seat)
    {
        Driver d = new Driver();
        d.Id = id;
        d.Name = name;
        d.CarClass = cls;
        d.Plate = plate;
        d.Phone = "+38067000" + (1000 + id);
        d.Rating = rating;
        d.X = x;
        d.Y = y;
        d.HasChildSeat = seat;
        Drivers.Add(d);
    }

    // формат з адмінки: "ЗОНА=відсоток;..."
    public static void LoadZones(string cfg)
    {
        foreach (string part in cfg.Split(';'))
        {
            try
            {
                string[] kv = part.Split('=');
                ZoneSurge[kv[0].Trim().ToUpper()] = int.Parse(kv[1]) / 100.0;
            }
            catch (Exception) { }
        }
    }

    public Driver FindDriver(int id)
    {
        foreach (Driver d in Drivers)
            if (d.Id == id)
                return d;
        return null;
    }

    public Ride FindRide(int id)
    {
        for (int i = 0; i < Rides.Count; i++)
            if (Rides[i].Id == id)
                return Rides[i];
        return null;
    }

    public int count_waiting()
    {
        int n = 0;
        foreach (Ride r in Rides)
            if (r.Status == RideStatus.Created)
                n++;
        return n;
    }
}

// Журнал у TEMP: на касових ПК немає прав на диск D
public class AuditLog
{
    public static string FilePath = Path.Combine(Path.GetTempPath(), "taxi_dispatch.log");

    public static void Write(string line)
    {
        try
        {
            File.AppendAllText(FilePath, DateTime.Now.ToString("HH:mm:ss") + " " + line + "\n");
        }
        catch { }
    }
}
