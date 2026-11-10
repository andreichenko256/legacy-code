namespace FleetMaintenance;

// Наряди: відкрити, почати, відкласти, закрити
public class WorkOrderService
{
    private FleetRepository repo;
    public string lastMessage = "";

    public WorkOrderService(FleetRepository repo)
    {
        this.repo = repo;
    }

    // inDays - через скільки днів машина стає в бокс
    public WorkOrder Open(string vehicleNo, string kind, string priority, double estimate,
        int inDays, string note)
    {
        Vehicle v = repo.FindVehicle(vehicleNo);
        if (v == null || v.Status == "SOLD" || kind == null || kind.Trim() == "")
        {
            lastMessage = "Cannot open order for '" + vehicleNo + "', kind '" + kind + "'";
            return null;
        }
        WorkOrder wo = new WorkOrder { VehicleNo = v.Number, Kind = kind.ToUpper() };
        wo.Number = "WO-" + FleetRepository.nextOrderNo;
        FleetRepository.nextOrderNo++;
        wo.Priority = priority;
        wo.PlannedDate = DateTime.Today.AddDays(inDays);
        wo.Estimate = estimate;
        wo.Note = note;
        repo.Orders.Add(wo);
        AuditLog.Write("open " + wo.Number + " " + v.Number + " " + wo.Kind);
        return wo;
    }

    // повертає false, якщо наряд не можна почати
    public bool Start(string orderNo, string mechanic)
    {
        WorkOrder wo = repo.findOrder(orderNo);
        if (wo == null)
        {
            lastMessage = "Work order " + orderNo + " not found";
            Console.WriteLine("  ! " + lastMessage);
            return false;
        }
        Vehicle v = repo.FindVehicle(wo.VehicleNo);
        if (wo.Status == WorkOrderStatus.Done)
            throw new InvalidOperationException("Work order " + wo.Number + " is already done");
        wo.Status = WorkOrderStatus.InService;
        wo.Mechanic = mechanic;
        v.Status = "WORKSHOP";
        AuditLog.Write("start " + wo.Number + " " + mechanic);
        return true;
    }

    public bool Postpone(string orderNo, int days, string reason)
    {
        WorkOrder wo = repo.findOrder(orderNo);
        if (wo == null)
        {
            lastMessage = "Work order " + orderNo + " not found";
            Console.WriteLine("  ! " + lastMessage);
            return false;
        }
        Vehicle v = repo.FindVehicle(wo.VehicleNo);
        if (wo.Status == WorkOrderStatus.Done)
        {
            lastMessage = "Work order " + wo.Number + " is already done";
            return false;
        }
        if (days <= 0)
        {
            lastMessage = "Postpone days must be positive";
            return false;
        }
        // термінове на вантажівці - не більше 3 днів
        if (wo.Priority == "URGENT" && v.Type == "TRUCK" && days > 3)
            days = 3;
        wo.Status = WorkOrderStatus.Postponed;
        wo.PlannedDate = wo.PlannedDate.AddDays(days);
        wo.PostponeCount++;
        v.Status = "ACTIVE";
        AuditLog.Write("postpone " + wo.Number + " +" + days + " " + reason);
        return true;
    }

    // Повертає суму або 0, якщо наряд не знайдено
    public double Complete(string orderNo, double hours, double partsCost, int odometer)
    {
        WorkOrder wo = repo.findOrder(orderNo);
        if (wo == null || hours <= 0)
        {
            lastMessage = wo == null ? "Work order " + orderNo + " not found" : "Invalid hours";
            return -1;
        }
        if (wo.Status == WorkOrderStatus.Done || wo.Status == WorkOrderStatus.Postponed)
        {
            lastMessage = "Work order " + wo.Number + " is " + wo.Status;
            return -1;
        }
        Vehicle v = repo.FindVehicle(wo.VehicleNo);
        double rate = 600; // нормогодина, грн
        if (v.Type == "TRUCK")
            rate = 950;
        else if (v.Type == "VAN")
            rate = 750;
        double labour = hours * rate;
        if (hours > 8)
            labour = labour + (hours - 8) * rate * 0.5; // понаднормові
        if (wo.Priority == "URGENT")
            labour = labour * 1.25;
        double sum = Math.Round(labour + partsCost * 1.15, 2); // 15% - націнка складу
        wo.Hours = hours;
        wo.Total = sum;
        wo.Status = WorkOrderStatus.Done;
        if (odometer > v.Mileage)
            v.Mileage = odometer;
        v.Status = "ACTIVE";
        repo.History.Add(new ServiceRecord { VehicleNo = v.Number, Kind = wo.Kind,
            Date = DateTime.Today, Mileage = v.Mileage, Cost = sum });
        AuditLog.Write("done " + wo.Number + " " + sum);
        return sum;
    }

    public void PrintOrders()
    {
        int open = 0;
        foreach (WorkOrder wo in repo.Orders)
        {
            Console.WriteLine("  " + wo.Number + " " + wo.VehicleNo + " " + wo.Kind.PadRight(11)
                + wo.Priority.PadRight(7) + wo.Status.ToString().PadRight(10) + wo.Total);
            if (wo.Status != WorkOrderStatus.Done)
                open++;
        }
        Console.WriteLine("Open orders: " + open);
    }
}
