namespace RepairService;

// Заявки в пам'яті. TODO: перейти на БД
public class RequestRepository
{
    public static List<RepairRequest> All = new List<RepairRequest>();
    private static int nextId = 1000;

    public RepairRequest Add(RepairRequest r)
    {
        nextId++;
        r.Id = nextId;
        // тимчасово: заявки з журналу мають свою дату
        if (r.ReceivedAt == DateTime.MinValue)
            r.ReceivedAt = DateTime.Now;
        All.Add(r);
        AuditLog.Write("request #" + r.Id + " added, " + r.DeviceType);
        return r;
    }

    public RepairRequest Find(int id)
    {
        foreach (RepairRequest r in All)
        {
            if (r.Id == id)
                return r;
        }
        return null;
    }

    // постійний клієнт - шукаємо по телефону
    public Customer FindCustomer(string phone)
    {
        foreach (RepairRequest r in All)
        {
            if (r.Customer != null && r.Customer.Phone == phone)
                return r.Customer;
        }
        return null;
    }
}
