namespace RepairService;

// Статуси заявки (константи додали у 2021)
public static class Statuses
{
    public const string New = "New";
    public const string Diagnosed = "Diagnosed";
    public const string WaitingParts = "WaitingParts";
    public const string InRepair = "InRepair";
    public const string Ready = "Ready";
    public const string Delivered = "Delivered";
    public const string Cancelled = "Cancelled";
    public const string Warranty = "Warranty";
}

public class Customer
{
    public string Name;
    public string Phone;
}

// Заявка на ремонт - основний документ
public class RepairRequest
{
    public int Id;
    public Customer Customer;
    public string DeviceType;   // Phone, Tablet, Laptop, PC, TV, Washer, Fridge
    public string Model;
    public string Problem;
    public string Priority = "Normal";   // Normal / Express
    public string Status = "New";
    public string Technician;
    public decimal Estimate;
    public decimal PartsCost;
    public decimal Total;
    public decimal Paid = 0;
    public bool Approved;
    public bool IsWarranty;
    public DateTime ReceivedAt;
    public DateTime DiagnosedAt;
    public DateTime StartedAt;
    public DateTime FinishedAt;
    public DateTime DeliveredAt;
    public DateTime WarrantyUntil;
    public List<string> History = new List<string>();
}
