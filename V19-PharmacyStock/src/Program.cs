namespace PharmacyStock;

// Демо-зміна аптеки: той самий сценарій після оновлень
public class Program
{
    public static void Main(string[] args)
    {
        StockRepository repo = LoadCatalog();
        PrescriptionService svc = new PrescriptionService(repo);
        Console.WriteLine("=== Prescriptions ===");
        Issue(svc, "Olena Kovalenko", "PENSIONER", "+380671112233", "NUR200", 2);
        Issue(svc, "Petro Melnyk", "", "0501234567", "TRM50", 4);
        Issue(svc, "Ivan Bondar", "DISABLED", "0937778899", "MET850", 3);
        Issue(svc, "Hanna Shevchuk", "", "0661110000", "AMX500", 3);
        Issue(svc, "Taras Lysenko", "", "0679990011", "LOS50", 1);
        Issue(svc, "Maria Tkachenko", "PENSIONER", "0632223344", "LOS50", 2);
        Issue(svc, "Oksana Hrytsenko", "", "0985556677", "LOS50", 2);
        Issue(svc, "Yurii Marchenko", "", "0500000001", "AMX500", 0);
        Issue(svc, "Yurii Marchenko", "", "0500000001", "ASP100", 1);
        // архівні рецепти: виписані 45 і рівно 30 днів тому
        repo.FindPrescription("RX-000005").IssuedOn = DateTime.Today.AddDays(-45);
        repo.FindPrescription("RX-000007").IssuedOn = DateTime.Today.AddDays(-30);
        Dispensing(svc);
        Console.WriteLine("=== Goods receipt ===");
        InventoryService inv = new InventoryService(repo);
        inv.Receive("AMX500", "A-2611", DateTime.Today.AddDays(400), 20, 61.00m);
        inv.Receive("MET850", "M-2690", DateTime.Today.AddDays(540), 10, 62.00m);
        inv.Receive("MET850", "M-0777", DateTime.Today.AddDays(30), 10, 40.00m);
        inv.Receive("TRM50", "", DateTime.Today.AddDays(400), 5, 70.00m);
        inv.Receive("IBU200", "I-1201", DateTime.Today.AddDays(700), 45, 21.00m);
        Console.WriteLine("=== Reports ===");
        ReportPrinter rep = new ReportPrinter(repo);
        rep.PrintReceipt("RX-000001");
        rep.PrintStock();
        rep.print_day();
        Console.WriteLine("Dispense calls: " + PrescriptionService.DispenseCount);
    }

    private static void Dispensing(PrescriptionService svc)
    {
        Console.WriteLine("=== Dispensing ===");
        Serve(svc, "RX-000001", 2, true, "PH-07");
        Serve(svc, "RX-000002", 4, false, "");
        Serve(svc, "RX-000002", 4, false, "PH-07");
        Serve(svc, "RX-000002", 0, false, "PH-07");
        Serve(svc, "RX-000003", 3, true, "PH-02");
        Serve(svc, "RX-000004", 3, false, "PH-02");
        Serve(svc, "RX-000005", 1, false, "PH-02");
        Serve(svc, "RX-000007", 2, false, "PH-02");
        Serve(svc, "RX-999999", 1, false, "PH-02");
        Console.WriteLine("=== Status ===");
        Console.WriteLine("Cancel RX-000006: "
            + svc.ChangeStatus("RX-000006", PrescriptionStatus.Cancelled));
        Serve(svc, "RX-000006", 2, true, "PH-02");
        Console.WriteLine("Cancel RX-000001: "
            + svc.ChangeStatus("RX-000001", PrescriptionStatus.Cancelled));
    }

    private static StockRepository LoadCatalog()
    {
        StockRepository repo = new StockRepository();
        repo.Medicines.Add(new Medicine("NUR200", "Nurofen 200mg N24", "ibuprofen", 145.00m));
        repo.Medicines.Add(new Medicine("IBU200", "Ibuprofen 200mg N50", "ibuprofen", 42.00m));
        repo.Medicines.Add(new Medicine("AMX500", "Amoxil 500mg N20", "amoxicillin", 96.00m));
        repo.Medicines.Add(new Medicine("MET850", "Metformin 850mg N60", "metformin", 74.30m));
        repo.Medicines.Add(new Medicine("TRM50", "Tramadol 50mg N10", "tramadol", 120.00m));
        repo.Medicines[4].IsControlled = true;
        repo.Medicines[4].MaxPerDispense = 2;
        repo.Medicines.Add(new Medicine("LOS50", "Lozap 50mg N30", "losartan", 128.00m));
        DateTime d = DateTime.Today;   // залишки на ранок (з 1С)
        repo.AddBatch("IBU200", "I-1180", d.AddDays(300), 10);
        repo.AddBatch("AMX500", "A-0815", d.AddDays(-4), 3);
        repo.AddBatch("AMX500", "A-0907", d.AddDays(60), 1);
        repo.AddBatch("MET850", "M-0702", d.AddDays(500), 10);
        repo.AddBatch("MET850", "M-0410", d.AddDays(-10), 5);
        repo.AddBatch("MET850", "M-0533", d.AddDays(20), 2);
        repo.AddBatch("TRM50", "T-0099", d.AddDays(365), 6);
        return repo;
    }

    private static void Issue(PrescriptionService svc, string name, string cat, string phone,
        string code, int qty)
    {
        try
        {
            string no = svc.Issue(name, cat, phone, code, qty, 30, "Dr. Hnatiuk");
            if (no == null)
                no = "rejected (" + svc.LastMessage + ")";
            Console.WriteLine(name + ": " + code + " x" + qty + " -> " + no);
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine(name + ": " + code + " x" + qty + " -> error: " + ex.Message);
        }
    }

    private static void Serve(PrescriptionService svc, string no, int q, bool x, string who)
    {
        decimal sum = svc.Dispense(no, q, x, who);
        Prescription p = svc.repo.FindPrescription(no);
        if (sum < 0 || svc.LastMessage != "")
            Console.WriteLine(no + " refused: " + svc.LastMessage);
        else
            Console.WriteLine(no + " -> " + p.Status + " " + p.DispensedQty + "/" + p.Quantity
                + ", to pay " + sum.ToString("0.00"));
    }
}
