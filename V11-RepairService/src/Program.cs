namespace RepairService;

public class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine("=== RepairService front desk ===");
        RequestRepository repo = new RequestRepository();
        RequestFlow flow = new RequestFlow(repo);
        RequestPrinter printer = new RequestPrinter();
        int a = flow.RegisterRequest("Olena Kovalenko", "067 123-45-67", "Phone", "Samsung A52",
            "Broken screen", false);
        int b = flow.RegisterRequest("Ihor Melnyk", "050-765-43-21", "Laptop", "Lenovo T480",
            "Does not boot", true);
        int c = flow.RegisterRequest("Mykola Bondarenko", "093 555 12 12", "TV", "Samsung UE43",
            "No picture", false);
        repo.Find(c).ReceivedAt = DateTime.Today.AddDays(-12); // з журналу
        try
        {
            flow.RegisterRequest("Petro", "12-34", "TV", "LG 43UK", "No sound", false);
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine("Registration error: " + ex.Message);
        }
        PhoneRepair(flow, repo, printer, a);
        OtherRequests(flow, repo, printer, b, c);
        Console.WriteLine("-- errors --");
        Step(flow, 9999, "Ready", "desk", 0, false);
        Console.WriteLine("Payment for #9999: " + flow.RegisterPayment(9999, 100));
        Console.WriteLine("Payment of 0 for #" + c + ": " + flow.RegisterPayment(c, 0));
        repo.Find(a).WarrantyUntil = DateTime.Today.AddDays(-1); // expired
        ShowActions(flow, repo, a);
        Step(flow, a, "Warranty", "desk", 0, false);
        printer.PrintDailyReport(RequestRepository.All);
        printer.PrintTechReport(RequestRepository.All, "sydorenko");
        Console.WriteLine("Status changes: " + RequestFlow.changesToday
            + ", sms sent: " + RequestFlow.smsToday);
    }

    static void PhoneRepair(RequestFlow flow, RequestRepository repo, RequestPrinter pr, int id)
    {
        Console.WriteLine("-- #" + id + " phone, broken screen --");
        ShowActions(flow, repo, id);
        Step(flow, id, "Diagnosed", "sydorenko", 1150, true);
        Step(flow, id, "InRepair", "sydorenko", 0, false);
        Step(flow, id, "Ready", "sydorenko", 0, true);
        Console.WriteLine("Debt: " + flow.RegisterPayment(id, 1000).ToString("0.00"));
        Step(flow, id, "Delivered", "desk", 0, false);
        Console.WriteLine("Debt: " + flow.RegisterPayment(id, 700).ToString("0.00"));
        Step(flow, id, "Delivered", "desk", 0, false);
        ShowActions(flow, repo, id);
        Step(flow, id, "Warranty", "desk", 0, false);
        Step(flow, id, "InRepair", "sydorenko", 0, false);
        Step(flow, id, "Ready", "sydorenko", 0, false);
        Step(flow, id, "Delivered", "desk", 0, false);
        pr.PrintCard(repo.Find(id));
    }

    static void OtherRequests(RequestFlow flow, RequestRepository repo, RequestPrinter pr,
        int b, int c)
    {
        Console.WriteLine("-- #" + b + " laptop, express --");
        Step(flow, b, "Ready", "bondar", 0, false);
        Step(flow, b, "Diagnosed", "bondar", -300, false);
        Step(flow, b, "Diagnosed", "bondar", 2500, true);
        Step(flow, b, "InRepair", "bondar", 0, false);
        Step(flow, b, "Cancelled", "desk", 0, true);
        pr.PrintCard(repo.Find(b));
        Console.WriteLine("-- #" + c + " TV, received 12 days ago --");
        Step(flow, c, "Diagnosed", "bondar", 2400, false);
        Console.WriteLine("Approved by phone: " + flow.Approve(c));
        Step(flow, c, "InRepair", "bondar", 0, false);
        Step(flow, c, "WaitingParts", "bondar", 350, false);
        Step(flow, c, "InRepair", "bondar", 0, false);
        Step(flow, c, "Ready", "bondar", 0, false);
        pr.PrintCard(repo.Find(c));
    }

    static void Step(RequestFlow flow, int id, string status, string user, decimal parts, bool sms)
    {
        bool ok = flow.ChangeStatus(id, status, user, parts, sms);
        Console.WriteLine("#" + id + " -> " + status + ": " + (ok ? "OK" : "FAILED"));
    }

    static void ShowActions(RequestFlow flow, RequestRepository repo, int id)
    {
        List<string> acts = flow.GetAllowedActions(repo.Find(id));
        string s = acts.Count == 0 ? "(none)" : string.Join(", ", acts);
        Console.WriteLine("  allowed for #" + id + ": " + s);
    }
}
