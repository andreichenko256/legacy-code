namespace TaxiDispatch;

// Диспетчер: замовлення, призначення водіїв, статуси
public class Dispatcher
{
    // черга на стоянках - вимкнули після переходу на GPS
    public static bool UseZoneQueue = false;
    private DispatchStore repo;
    private FareCalculator fares = new FareCalculator();
    public string LastNote;     // причина відмови для оператора

    public Dispatcher(DispatchStore store)
    {
        repo = store;
    }

    // new order from operator or website; returns id or -1
    public int CreateRide(string name, string phone, double px, double py, double dx, double dy,
        string carClass, bool childSeat, string zone)
    {
        LastNote = "";
        if (name == null || name.Trim().Length == 0)
        {
            LastNote = "name is empty";
            return -1;
        }
        string p = phone.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");
        if (p.StartsWith("0"))
            p = "+38" + p;
        if (p.Length != 13)
        {
            LastNote = "invalid phone " + phone;
            return -1;
        }
        // temp: old website sends "standard", "eco", "" - treat as ECO
        string cls = carClass == null ? "ECO" : carClass.Trim().ToUpper();
        if (cls != "COMFORT" && cls != "BUSINESS" && cls != "VAN")
            cls = "ECO";
        Ride r = new Ride();
        r.Id = DispatchStore.nextRideId++;
        r.Passenger = name.Trim();
        r.Phone = p;
        r.PickupX = px;
        r.PickupY = py;
        r.DropX = dx;
        r.DropY = dy;
        r.CarClass = cls;
        r.ChildSeat = childSeat;
        r.Zone = zone == null ? "CENTER" : zone.Trim().ToUpper();
        repo.Rides.Add(r);
        AuditLog.Write("new ride #" + r.Id + " " + p + " " + cls);
        return r.Id;
    }

    // Призначення водія. Повертає Id водія або
    // -1 (немає замовлення), -2 (вже не нове), -3 (немає машини)
    public int Assign(int rideId, double maxKm, bool allowUpgrade)
    {
        LastNote = "";
        Ride r = repo.FindRide(rideId);
        if (r == null)
        {
            LastNote = "ride #" + rideId + " not found";
            return -1;
        }
        if (r.Status != RideStatus.Created)
        {
            LastNote = "ride #" + r.Id + " is " + r.Status;
            return -2;
        }
        // коефіцієнт: зона + нестача машин
        double k = 1.0;
        if (DispatchStore.ZoneSurge.ContainsKey(r.Zone))
            k = DispatchStore.ZoneSurge[r.Zone];
        int waiting = repo.count_waiting();
        int freeCnt = CountFree();
        if (waiting > freeCnt)
        {
            k = k + 0.15 * (waiting - freeCnt);
            if (IsRushHour())
                k = k + 0.2;
        }
        r.Surge = Math.Min(k, 2.5);
        //double k = 1 + repo.CountWaiting() * 0.05;
        //if (r.Zone == "AIRPORT")
        //    k = k + 0.3;
        //if (k > 3) k = 3;
        //r.Surge = k;

        // попередня ціна - водій бачить її до прийняття
        double est = fares.Estimate(r.CarClass,
            Km(r.PickupX, r.PickupY, r.DropX, r.DropY) * 1.3);

        Driver best = null;
        double bestKm = maxKm;
        if (UseZoneQueue)
        {
            // стоянка: бере перший у черзі
            best = repo.Drivers.Find(x => x.Zone == r.Zone);
            r.DriverId = best.Id;
            r.Status = RideStatus.Assigned;
            r.AssignedAt = DateTime.Now;
            best.Status = "onride";
            best.ActiveRides++;
            AuditLog.Write("ride #" + r.Id + " -> driver " + best.Id + ", surge " + r.Surge);
            return best.Id;
        }

        // мін. рейтинг 4.0, бізнес - від 4.7 включно
        double minRating = r.CarClass == "BUSINESS" ? 4.7 : 4.0;
        foreach (Driver d in repo.Drivers)
        {
            if (d.CarClass != r.CarClass || d.Rating <= minRating)
                continue;
            // вільний або везе, але бере ще одне (до 2)
            if (d.Status == "free" || d.Status == "onride" && d.ActiveRides < 2)
            {
                if (!r.ChildSeat || d.HasChildSeat)
                {
                    double dist = Km(d.X, d.Y, r.PickupX, r.PickupY);
                    //double dist = Math.Abs(d.X - r.PickupX) + Math.Abs(d.Y - r.PickupY);
                    //if (d.Status == "onride")
                    //    dist = dist + 2;
                    if (dist <= bestKm)
                    {
                        best = d;
                        bestKm = dist;
                    }
                }
            }
        }
        /* старий вибір (до 2023): найкращий рейтинг у радіусі
        foreach (Driver d in repo.Drivers)
            if (d.CarClass == r.CarClass && d.Status == "free"
                && Km(d.X, d.Y, r.PickupX, r.PickupY) <= maxKm)
                if (best == null || d.Rating > best.Rating)
                    best = d;
        */

        // економу немає - даємо комфорт за ціною економу
        if (best == null && allowUpgrade && r.CarClass == "ECO")
        {
            r.CarClass = "COMFORT";
            Console.WriteLine("  upgrade: ride #" + r.Id + " ECO -> COMFORT");
            return Assign(rideId, maxKm, false);
        }
        if (best == null)
        {
            LastNote = "no car for ride #" + r.Id + " within " + maxKm + " km";
            return -3;
        }
        r.DriverId = best.Id;
        r.PickupKm = Math.Round(bestKm, 1);
        r.Status = RideStatus.Assigned;
        r.AssignedAt = DateTime.Now;
        best.Status = "onride";
        best.ActiveRides++;
        AuditLog.Write("ride #" + r.Id + " -> driver " + best.Id + ", surge " + r.Surge);
        return best.Id;
    }

    // додаток водія: на місці -> в дорозі -> завершено
    public bool ChangeStatus(int rideId, RideStatus to)
    {
        Ride r = repo.FindRide(rideId);
        if (r == null)
            return false;
        bool ok = false;
        switch (r.Status)
        {
            case RideStatus.Created:
                ok = to == RideStatus.Cancelled;    // призначення - лише Assign
                break;
            case RideStatus.Assigned:
                ok = to == RideStatus.InProgress || to == RideStatus.Cancelled;
                break;
            case RideStatus.InProgress:
                ok = to == RideStatus.Completed;
                break;
        }
        if (!ok)
        {
            Console.WriteLine("  ! ride #" + r.Id + ": " + r.Status + " -> " + to + " rejected");
            return false;
        }
        Driver d = repo.FindDriver(r.DriverId);
        if (to == RideStatus.InProgress)
            r.StartedAt = DateTime.Now;
        if (to == RideStatus.Completed)
        {
            r.Fare = fares.Calc(r);
            d.ActiveRides--;
            if (d.ActiveRides == 0)
                d.Status = "free";
            d.X = r.DropX;     // водій там, де висадив
            d.Y = r.DropY;
            d.TripsToday++;
            d.EarnedToday = d.EarnedToday + fares.DriverPayout(d, r.Fare);
        }
        if (to == RideStatus.Cancelled)
            r.Fare = fares.CancelFee(r);   // водія звільняє додаток
        r.Status = to;
        AuditLog.Write("ride #" + r.Id + " " + to);
        return true;
    }

    public int CountFree()
    {
        int n = 0;
        foreach (Driver d in repo.Drivers)
            if (d.Status == "free")
                n++;
        return n;
    }

    // rush hour 7-10 and 17-20 (local time)
    private bool IsRushHour()
    {
        int h = DateTime.Now.Hour;
        return h >= 7 && h < 10 || h >= 17 && h < 20;
    }

    public static double Km(double x1, double y1, double x2, double y2)
    {
        return Math.Sqrt((x1 - x2) * (x1 - x2) + (y1 - y2) * (y1 - y2));
    }

    // бали для старого вибору: менше - краще
    private double ScoreDriver(Driver d, double dist)
    {
        double s = dist * 10 - d.Rating * 3;
        if (d.TripsToday > 12)
            s = s + 5;
        if (d.HasChildSeat && d.Status != "free")
            s = s + 2;
        return s;
    }
}
