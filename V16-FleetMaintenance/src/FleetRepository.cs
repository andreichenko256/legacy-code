namespace FleetMaintenance;

// Сховище парку в пам'яті (БД поки нема)
public class FleetRepository
{
    public static int nextOrderNo = 1001;
    public static bool MilesImport = false; // old imports with odometer in miles
    public List<Vehicle> Vehicles = new List<Vehicle>();
    public List<ServiceRecord> History = new List<ServiceRecord>();
    public List<WorkOrder> Orders = new List<WorkOrder>();

    // з таблиці завгара: номер, далі вид:днів тому:пробіг
    private const string HistoryCsv = @"
AI4512KA OIL:220:346000 OIL:40:376800 BRAKES:300:350000 TYRES:500:360000 INSPECTION:190:392000
AI7730KB OIL:60:250200 BRAKES:400:230000 TYRES:300:215000 INSPECTION:100:255000
AA3318OP OIL:100:179900 BRAKES:800:160000 TYRES:200:150000 INSPECTION:300:170000
AA5521OP OIL:200:85000 BRAKES:760:52000 TYRES:300:70000 INSPECTION:90:90000
AA2045MX OIL:280:108000 BRAKES:500:80000 TYRES:1500:30000 INSPECTION:365:109000
AA8806MX OIL:150:36000 INSPECTION:200:41000
AA7001TT OIL:120:131000 BRAKES:120:131000 INSPECTION:340:118000
AA1290EK OIL:n/a:351200
";

    public FleetRepository()
    {
        AddVehicle("AI4512KA", "TRUCK", "MAN", "TGS 18.440", 2016, 412300, 2900);
        AddVehicle("AI7730KB", "TRUCK", "DAF", "XF 480", 2019, 268500, 2100);
        AddVehicle("AA3318OP", "VAN", "Renault", "Master", 2018, 186400, 2400);
        AddVehicle("AA5521OP", "VAN", "Ford", "Transit", 2021, 94200, 1300);
        AddVehicle("AA2045MX", "CAR", "Skoda", "Octavia", 2020, 121800, 2000);
        AddVehicle("AA8806MX", "CAR", "Toyota", "Camry", 2022, 38700, 600).Tyres = "SUMMER";
        AddVehicle("AA7001TT", "MINIBUS", "Mercedes", "Sprinter", 2023, 152000, 700);
        AddVehicle("AA1290EK", "CAR", "Daewoo", "Lanos", 2009, 356000, 5000).Status = "SOLD";
        LoadHistory(HistoryCsv);
    }

    public Vehicle AddVehicle(string no, string type, string make, string model, int year,
        int km, int ownedDays)
    {
        Vehicle v = new Vehicle { Number = no, Type = type, Make = make, Model = model };
        v.Year = year;
        v.Mileage = km;
        if (MilesImport)
            v.Mileage = (int)(km * 1.609);
        v.Purchased = DateTime.Today.AddDays(-ownedDays);
        Vehicles.Add(v);
        return v;
    }

    private void LoadHistory(string csv)
    {
        foreach (string line in csv.Split('\n'))
        {
            string[] parts = line.Trim().Split(' ');
            for (int i = 1; i < parts.Length; i++)
            {
                string[] p = parts[i].Split(':');
                try
                {
                    ServiceRecord r = new ServiceRecord { VehicleNo = parts[0], Kind = p[0] };
                    r.Date = DateTime.Today.AddDays(-int.Parse(p[1]));
                    r.Mileage = int.Parse(p[2]);
                    History.Add(r);
                }
                catch (Exception) { }
            }
        }
    }

    public Vehicle FindVehicle(string no)
    {
        if (no == null)
            return null;
        // TODO: тимчасово, номери з пробілами
        no = no.Replace(" ", "").ToUpper();
        foreach (Vehicle v in Vehicles)
            if (v.Number == no)
                return v;
        return null;
    }

    // остання робота цього виду, інакше - день купівлі
    public ServiceRecord LastService(string vehicleNo, string kind)
    {
        ServiceRecord last = null;
        foreach (ServiceRecord r in History)
            if (r.VehicleNo == vehicleNo && r.Kind == kind)
                if (last == null || r.Date > last.Date)
                    last = r;
        if (last == null)
            last = new ServiceRecord { VehicleNo = vehicleNo, Kind = kind,
                Date = FindVehicle(vehicleNo).Purchased };
        return last;
    }

    public WorkOrder findOrder(string no)
    {
        foreach (WorkOrder wo in Orders)
            if (wo.Number == no)
                return wo;
        return null;
    }

    public bool HasOpenOrder(string vehicleNo, string kind)
    {
        foreach (WorkOrder wo in Orders)
            if (wo.VehicleNo == vehicleNo && wo.Kind == kind && wo.Status != WorkOrderStatus.Done)
                return true;
        return false;
    }
}
