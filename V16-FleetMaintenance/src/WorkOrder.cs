namespace FleetMaintenance;

public enum WorkOrderStatus { Planned, InService, Done, Postponed }

// Наряд на роботу в боксі
public class WorkOrder
{
    public string Number;               // WO-1001 ...
    public string VehicleNo;
    public string Kind;
    public string Priority;             // URGENT / DUE
    public WorkOrderStatus Status = WorkOrderStatus.Planned;
    public DateTime PlannedDate;
    public string Mechanic;
    public double Hours;
    public double Estimate;
    public double Total;
    public string Note = "";
    public int PostponeCount;
}

// Рядок плану ТО
public class PlanItem
{
    public string Kind;
    public string Priority = "DUE";
    public int KmOver, DaysOver;        // понад інтервал, мінус - запас
    public double Estimate;
    public string OrderNo;
    public string Note;
}
