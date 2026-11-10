namespace RepairService;

// Робота із заявкою: прийом, статуси, оплата
public class RequestFlow
{
    private RequestRepository repo;
    private CostCalculator calc;
    private Notifier notifier;
    private RequestPrinter printer;
    public static int changesToday = 0;
    public static int smsToday = 0;

    public RequestFlow() : this(new RequestRepository()) { }

    public RequestFlow(RequestRepository repository)
    {
        repo = repository;
        calc = new CostCalculator();
        notifier = new Notifier();
        printer = new RequestPrinter();
    }

    // прийом техніки, повертає номер заявки
    public int RegisterRequest(string name, string phone, string device, string model,
        string problem, bool express)
    {
        if (name == null || name.Trim() == "")
            throw new ArgumentException("customer name is empty");
        if (phone == null || phone.Replace(" ", "").Replace("-", "").Length < 10)
            throw new ArgumentException("bad phone number '" + phone + "'");
        Customer c = repo.FindCustomer(phone);
        if (c == null)
            c = new Customer { Name = name, Phone = phone };
        RepairRequest r = new RepairRequest();
        r.Customer = c;
        r.DeviceType = device;
        r.Model = model;
        r.Problem = problem;
        if (express)
            r.Priority = "Express";
        repo.Add(r);
        return r.Id;
    }

    // Зміна статусу. Метод великий, але так задумано:
    // усі переходи в одному місці, нічого не загубиться.
    // Дозволені переходи - див. GetAllowedActions.
    public bool ChangeStatus(int id, string newStatus, string user, decimal partsCost, bool sms)
    {
        RepairRequest r = repo.Find(id);
        if (r == null)
        {
            Console.WriteLine("ChangeStatus: request #" + id + " not found");
            return false;
        }
        string old = r.Status;
        if (!GetAllowedActions(r).Contains(newStatus))
        {
            Console.WriteLine("Request #" + id + ": " + old + " -> " + newStatus
                + " is not allowed");
            return false;
        }
        DateTime now = DateTime.Now;
        switch (newStatus)
        {
            case "New":
                // повернення в New прибрали у 2022
                r.Estimate = 0;
                r.Approved = false;
                break;
            case "Diagnosed":
                if (partsCost < 0)
                {
                    Console.WriteLine("Request #" + id + ": parts cost can not be negative");
                    return false;
                }
                r.DiagnosedAt = now;
                r.Technician = user;
                r.PartsCost = partsCost;
                r.Estimate = calc.calcEst(r.DeviceType, r.Priority, partsCost);
                // дорожче 3000 - тільки після згоди клієнта
                r.Approved = r.Estimate <= 3000;
                break;
            case "WaitingParts":
                r.PartsCost = r.PartsCost + partsCost;
                break;
            case "InRepair":
                if (!r.Approved)
                {
                    Console.WriteLine("Request #" + id + ": estimate is not approved");
                    return false;
                }
                r.Technician = user;
                r.StartedAt = now;
                break;
            case "Ready":
                r.FinishedAt = now;
                if (!r.IsWarranty)
                {
                    decimal labor = calc.LaborRate(r.DeviceType);
                    if (r.Priority == "Express")
                    {
                        // після 18:00 і в неділю - подвійний тариф
                        if (now.Hour >= 18 || now.DayOfWeek == DayOfWeek.Sunday)
                            labor = labor * 2;
                        else
                            labor = labor * 1.5m;
                    }
                    int d = (now.Date - CalcDeadline(r).Date).Days;
                    if (d >= 0)
                    {
                        // прострочка: -5% за кожен день, макс. 30%
                        decimal disc = Math.Min((d + 1) * 5, 30);
                        labor = labor - labor * disc / 100;
                    }
                    r.Total = Math.Round(labor + r.PartsCost, 2);
                }
                break;
            case "Delivered":
                if (r.Paid < r.Total)
                {
                    Console.WriteLine("Request #" + id + ": not paid, debt "
                        + (r.Total - r.Paid).ToString("0.00"));
                    return false;
                }
                r.DeliveredAt = now;
                // гарантія 6 місяців
                r.WarrantyUntil = now.AddDays(90);
                break;
            case "Cancelled":
                if (old != "New")
                    r.Total = 200; // діагностика платна
                break;
            case "Warranty":
                r.IsWarranty = true;
                r.Approved = true;
                break;
        }
        r.Status = newStatus;
        r.History.Add(now.ToString("dd.MM.yyyy HH:mm") + " " + old + " -> " + newStatus
            + " (" + user + ")");
        AuditLog.Write("#" + r.Id + " " + old + " -> " + newStatus + " by " + user);
        changesToday++;
        if (sms)
        {
            string msg = printer.StatusText(newStatus);
            string phone = r.Customer.Phone.Replace(" ", "").Replace("-", "");
            string text = "Request #" + r.Id + ": " + msg;
            notifier.SendSms(phone, text);
            smsToday++;
            r.History.Add(DateTime.Now.ToString("dd.MM.yyyy HH:mm") + " sms: " + msg);
            AuditLog.Write("#" + r.Id + " sms to " + phone);
        }
        return true;
    }

    // що можна зробити далі (кнопки на формі)
    public List<string> GetAllowedActions(RepairRequest r)
    {
        switch (r.Status)
        {
            case "New":
                return new List<string> { "Diagnosed", "Cancelled" };
            case "Diagnosed":
                return new List<string> { "WaitingParts", "InRepair", "Cancelled" };
            case "WaitingParts":
                return new List<string> { "InRepair", "Cancelled" };
            case "InRepair":
                // в ремонті скасувати вже не можна
                return new List<string> { "WaitingParts", "Ready" };
            case "Ready":
                return new List<string> { "Delivered" };
            case "Delivered":
                if (DateTime.Today <= r.WarrantyUntil.Date)
                    return new List<string> { "Warranty" };
                break;
            case "Warranty":
                return new List<string> { "InRepair" };
            case "Cancelled":
                break;
        }
        return new List<string>();
    }

    // нормативний термін для поточного етапу
    public DateTime CalcDeadline(RepairRequest r)
    {
        switch (r.Status)
        {
            case "New":
                return r.ReceivedAt.AddDays(1);
            case "Diagnosed":
                return r.ReceivedAt.AddDays(3);
            case "WaitingParts":
                return r.ReceivedAt.AddDays(21);
            case "InRepair":
                return r.ReceivedAt.AddDays(r.Priority == "Express" ? 2 : 10);
            case "Ready":
                return r.FinishedAt.AddDays(30); // далі зберігання платне
            case "Warranty":
                return r.DeliveredAt.AddDays(14);
            case "Delivered":
            case "Cancelled":
                return DateTime.MaxValue;
        }
        return DateTime.MaxValue;
    }

    // customer agreed with the estimate (phone or in person)
    public bool Approve(int id)
    {
        RepairRequest r = repo.Find(id);
        if (r == null || r.Status != "Diagnosed")
            return false;
        r.Approved = true;
        string msg = "estimate " + r.Estimate.ToString("0.00") + " approved";
        string phone = r.Customer.Phone.Replace(" ", "").Replace("-", "");
        string text = "Request #" + r.Id + ": " + msg;
        notifier.SendSms(phone, text);
        smsToday++;
        r.History.Add(DateTime.Now.ToString("dd.MM.yyyy HH:mm") + " sms: " + msg);
        AuditLog.Write("#" + r.Id + " sms to " + phone);
        return true;
    }

    // повертає борг, що лишився
    // -1 нема заявки, -2 погана сума, -3 ще не готово
    public decimal RegisterPayment(int id, decimal amount)
    {
        RepairRequest r = repo.Find(id);
        if (r == null)
            return -1;
        if (amount <= 0)
            return -2;
        if (r.Status != "Ready" && r.Status != "Delivered")
            return -3;
        r.Paid = r.Paid + amount;
        if (r.Paid > r.Total)
        {
            Console.WriteLine("  change: " + (r.Paid - r.Total).ToString("0.00"));
            r.Paid = r.Total;
        }
        string msg = "payment " + amount.ToString("0.00") + " received";
        string phone = r.Customer.Phone.Replace(" ", "").Replace("-", "");
        string text = "Request #" + r.Id + ": " + msg;
        notifier.SendSms(phone, text);
        smsToday++;
        r.History.Add(DateTime.Now.ToString("dd.MM.yyyy HH:mm") + " sms: " + msg);
        AuditLog.Write("#" + r.Id + " sms to " + phone);
        return r.Total - r.Paid;
    }
}
