namespace FleetMaintenance;

// Планувальник ТО. Інтервали (наказ по парку від 2019 р.):
// масло 10000 км / 6 міс., гальма 40000 км, шини 60000 км / 4 роки,
// техогляд раз на рік
public class MaintenancePlanner
{
    private FleetRepository repo;
    private WorkOrderService orders;
    public string lastError = "";
    private Vehicle current;   // for PrintPlan
    private double total;

    public MaintenancePlanner() : this(new FleetRepository()) { }

    public MaintenancePlanner(FleetRepository repo)
    {
        this.repo = repo;
        orders = new WorkOrderService(repo);
    }

    // heavy - місто/причіп; createOrders - наряди на термінове
    public List<PlanItem> Plan(string vehicleNo, int kmPerDay, bool heavy, bool createOrders)
    {
        Vehicle v = repo.FindVehicle(vehicleNo);
        if (v == null)
        {
            lastError = "Vehicle " + vehicleNo + " not found";
            return null;
        }
        current = v;
        total = 0;
        List<PlanItem> res = new List<PlanItem>();
        if (v.Status == "SOLD")
            return res;
        int oilKm = 15000, oilDays = 365, brakeKm = 45000, tyreKm = 60000, inspDays = 365;
        if (v.Type == "TRUCK")
        {
            oilKm = 30000; oilDays = 180; brakeKm = 60000; tyreKm = 100000;
            inspDays = 182; // ОТК двічі на рік
        }
        else if (v.Type == "VAN")
        {
            oilKm = 12000; brakeKm = 40000; tyreKm = 50000;
        }
        if (heavy)
        {
            // важкі умови - масло вдвічі частіше
            oilKm = oilKm / 2;
            oilDays = oilDays / 2;
        }
        int brakeDays = 730, tyreDays = 1460; // 2 and 4 years
        // масло: пройшло N км АБО M днів - що настане раніше
        ServiceRecord last = repo.LastService(v.Number, "OIL");
        if (v.Mileage - last.Mileage >= oilKm || (DateTime.Today - last.Date).Days >= oilDays)
        {
            PlanItem it = new PlanItem { Kind = "OIL" };
            it.KmOver = v.Mileage - last.Mileage - oilKm;
            it.DaysOver = (DateTime.Today - last.Date).Days - oilDays;
            it.Estimate = PartsEstimate("OIL", v);
            res.Add(it);
        }
        // гальма - так само
        last = repo.LastService(v.Number, "BRAKES");
        if (v.Mileage - last.Mileage >= brakeKm || (DateTime.Today - last.Date).Days >= brakeDays)
        {
            PlanItem it2 = new PlanItem { Kind = "BRAKES" };
            it2.KmOver = v.Mileage - last.Mileage - brakeKm;
            it2.DaysOver = (DateTime.Today - last.Date).Days - brakeDays;
            it2.Estimate = PartsEstimate("BRAKES", v);
            res.Add(it2);
        }
        last = repo.LastService(v.Number, "TYRES");
        if (v.Mileage - last.Mileage >= tyreKm || (DateTime.Today - last.Date).Days >= tyreDays)
        {
            PlanItem t = new PlanItem { Kind = "TYRES" };
            t.KmOver = v.Mileage - last.Mileage - tyreKm;
            t.DaysOver = (DateTime.Today - last.Date).Days - tyreDays;
            t.Estimate = PartsEstimate("TYRES", v);
            res.Add(t);
        }
        //last = repo.LastService(v.Number, "BELT"); // ГРМ, у нових - ланцюг
        //if (v.Mileage - last.Mileage >= 90000 || (DateTime.Today - last.Date).Days >= 1825)
        //    res.Add(new PlanItem { Kind = "BELT", Estimate = 5200 });
        // техогляд - тільки за датою
        last = repo.LastService(v.Number, "INSPECTION");
        if ((DateTime.Today - last.Date).Days > inspDays)
        {
            PlanItem insp = new PlanItem { Kind = "INSPECTION" };
            insp.DaysOver = (DateTime.Today - last.Date).Days - inspDays;
            insp.Estimate = PartsEstimate("INSPECTION", v);
            res.Add(insp);
        }
        // з жовтня - перевзути на зимову (TODO: навесні назад)
        if (v.Tyres == "SUMMER" && DateTime.Today.Month >= 10)
        {
            PlanItem sw = new PlanItem { Kind = "TYRES", Note = "seasonal swap" };
            sw.Estimate = 1200;
            res.Add(sw);
        }
        foreach (PlanItem p in res)
        {
            // прострочено на 2 тижні їзди або на місяць
            if (p.KmOver > kmPerDay * 14 || p.DaysOver > 30)
                p.Priority = "URGENT";
            total = total + p.Estimate;
            if (createOrders)
            {
                if (p.Priority == "URGENT")
                {
                    if (repo.HasOpenOrder(v.Number, p.Kind))
                    {
                        Console.WriteLine("  ! " + v.Number + " " + p.Kind + ": order is open");
                        p.Note = "order already open";
                        continue;
                    }
                    WorkOrder wo = orders.Open(v.Number, p.Kind, "URGENT", p.Estimate, 1, "auto");
                    p.OrderNo = wo.Number;
                }
            }
        }
        AuditLog.Write("plan " + v.Number + ": " + res.Count + " item(s), " + total);
        return res;
    }

    public void PrintPlan(List<PlanItem> items)
    {
        if (items == null)
        {
            Console.WriteLine("  ! " + lastError);
            return;
        }
        Console.WriteLine("Plan " + current.Number + " (" + current.Type + "):");
        foreach (PlanItem p in items)
        {
            string s = "  " + p.Kind.PadRight(11) + p.Priority.PadRight(7);
            if (p.KmOver > 0)
                s = s + " +" + p.KmOver + " km";
            if (p.DaysOver > 0)
                s = s + " +" + p.DaysOver + " d";
            Console.WriteLine((s.PadRight(36) + p.Estimate.ToString("F2").PadLeft(9) + "  "
                + p.OrderNo + p.Note).TrimEnd());
        }
        Console.WriteLine("  total: " + total.ToString("F2") + " UAH");
    }

    // робота + запчастини, грн. Прайс 2022, TODO: оновити
    public double PartsEstimate(string kind, Vehicle v)
    {
        double d;
        switch (kind)
        {
            case "OIL": d = 2400; break;
            case "BRAKES": d = 6800; break;
            case "TYRES": d = 16000; break;
            case "INSPECTION": d = 900; break;
            default: d = 0; break;
        }
        if (v.Type == "TRUCK")
            d = d * 2.5;
        else if (v.Type == "VAN")
            d = d * 1.4;
        if (v.Year <= 2012 || v.Mileage > 400000)
            d = d * 1.1; // на старих завжди щось вилазить
        return d;
    }
}
