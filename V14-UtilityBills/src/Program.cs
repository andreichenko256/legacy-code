namespace UtilityBills;

public class Program
{
    static BillingRepository repo = new BillingRepository();
    static BillCalculator calc = new BillCalculator(repo);
    static BillingService billing = new BillingService(repo);
    static ReportService reports = new ReportService(repo);

    public static void Main(string[] args)
    {
        DateTime t = DateTime.Today;   // дати в демо - відносно сьогодні
        DateTime y = t.AddDays(-1);
        Console.WriteLine("=== Zelenyi Hai housing co-op - utility billing ===");
        Console.WriteLine("accounts " + repo.Accounts.Count + ", bills " + repo.Bills.Count);
        Console.WriteLine("-- electricity steps, UB-1001 (single zone)");
        double[] steps = { 80, 100, 350, 600, 601, 750 };
        foreach (double k in steps)
            Show(k + " kWh", calc.Charge("UB-1001", "EL", 0, k, 0, y));
        Console.WriteLine("-- other charges");
        Show("UB-1002 EL 150 kWh", calc.Charge("UB-1002", "EL", 8650, 8800, 0, y));
        Show("UB-1005 EL 300, night 120", calc.Charge("UB-1005", "EL", 7700, 8000, 120, y));
        Show("UB-1001 GAS 45 m3", calc.Charge("UB-1001", "GAS", 3410, 3455, 0, y));
        Show("UB-1003 WATER 18 m3", calc.Charge("UB-1003", "WATER", 912, 930, 0, y));
        Show("UB-1002 EL, 60 days old",
            calc.Charge("UB-1002", "EL", 8650, 9100, 0, t.AddDays(-60)));
        Show("UB-1001 EL 5000 -> 4980", calc.Charge("UB-1001", "EL", 5000, 4980, 0, y));
        Show("UB-9999 EL", calc.Charge("UB-9999", "EL", 0, 100, 0, y));
        Show("UB-1001 EL 100 -> -5", calc.Charge("UB-1001", "EL", 100, -5, 0, y));
        BillFlow(y);
        OverdueAndForecast();
    }

    static void Show(string what, double amount)
    {
        string extra = "";
        if (calc.LastSubsidy > 0)
            extra = ", subsidy " + calc.LastSubsidy;
        if (calc.LastNote != "")
            extra = extra + " (" + calc.LastNote + ")";
        Console.WriteLine("  " + what + ": " + amount + extra);
    }

    static void Step(string what, bool ok)
    {
        if (ok)
            Console.WriteLine("  " + what + ": ok");
        else
            Console.WriteLine("  " + what + ": refused");
    }

    static int Created(Bill b)
    {
        if (b == null)
        {
            Console.WriteLine("  not created: " + billing.LastError);
            return 0;
        }
        Console.WriteLine("  created " + b.Number + " " + b.AccountNo + " " + b.Service + ": "
            + b.Units + " units, " + b.Amount + " UAH, " + b.Status);
        return b.Number;
    }

    static void BillFlow(DateTime y)
    {
        Console.WriteLine("-- bills");
        int b1 = Created(billing.CreateBill("UB-1001", "EL", 12000, 12240, 0, y, false));
        Step("pay draft " + b1, billing.Pay(b1, 868.8));
        Step("issue " + b1, billing.ChangeStatus(b1, BillStatus.Issued));
        Step("pay 500 of " + b1, billing.Pay(b1, 500));
        Step("pay 368.8 of " + b1, billing.Pay(b1, 368.8));
        Step("issue paid " + b1, billing.ChangeStatus(b1, BillStatus.Issued));
        Created(billing.CreateBill("UB-1003", "WATER", 912, 930, 0, y, true));
        int b3 = Created(billing.CreateBill("UB-1005", "EL", 7700, 8000, 120, y, false));
        Step("cancel " + b3, billing.ChangeStatus(b3, BillStatus.Cancelled));
        Step("pay cancelled " + b3, billing.Pay(b3, 100));
        Created(billing.CreateBill("UB-7777", "GAS", 0, 10, 0, y, false));
        Created(billing.CreateBill("UB-1004", "HEAT", 0, 40, 0, y, false));
        Created(billing.CreateBill("UB-1002", "EL", 8650, -10, 0, y, false));
        Step("status of bill 424242", billing.ChangeStatus(424242, BillStatus.Paid));
    }

    static void OverdueAndForecast()
    {
        Console.WriteLine("-- overdue check");
        Console.WriteLine("  marked overdue: " + billing.RefreshOverdue());
        reports.PrintBill(5001);
        reports.PrintBill(5002);
        Step("correct 5001 to 110 kWh", billing.CorrectBill(5001, 110));
        reports.PrintBill(5001);
        Step("correct water bill 5002", billing.CorrectBill(5002, 10));
        Step("pay 5002 in full", billing.Pay(5002, 600));
        reports.PrintBill(424242);
        Console.WriteLine("-- hotline forecast");
        reports.PrintForecast("UB-1002", 250);
        reports.PrintForecast("UB-1004", 650);
        reports.PrintForecast("UB-0000", 100);
    }
}
