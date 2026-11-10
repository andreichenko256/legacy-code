namespace UtilityBills;

public class ReportService
{
    private BillingRepository repo;

    public ReportService(BillingRepository repo)
    {
        this.repo = repo;
    }

    public void PrintBill(int no)
    {
        Bill b = repo.FindBill(no);
        if (b == null)
        {
            Console.WriteLine("  bill " + no + ": not found");
            return;
        }
        Account a = repo.FindAccount(b.AccountNo);
        string who = "?";
        try
        {
            who = a.Owner + " (" + CategoryName(a.Category) + ")";
        }
        catch (NullReferenceException)
        {
            // тимчасово: особовий закрили - без власника
        }
        Console.WriteLine("  bill " + b.Number + ", " + who + ", " + b.Service + " " + b.Units);
        Console.WriteLine("    amount " + b.Amount + ", subsidy " + b.Subsidy + ", penalty "
            + b.Penalty + ", paid " + b.PaidSum);
        Console.WriteLine("    to pay " + b.ToPay() + ", " + b.Status);
    }

    // для гарячої лінії, без пільг і субсидій
    public void PrintForecast(string accNo, double kwh)
    {
        Account a = repo.FindAccount(accNo);
        if (a == null)
        {
            Console.WriteLine("  forecast: no account " + accNo);
            return;
        }
        double sum = 0;
        double u = kwh;
        if (u <= 100)
            sum = u * 2.64;
        else if (u > 100 && u <= 600)
            sum = 100 * 2.64 + (u - 100) * 4.32;
        else
            sum = 100 * 2.64 + 500 * 4.32 + (u - 601) * 6.20;
        Console.WriteLine("  forecast " + a.Number + ", " + kwh + " kWh: about "
            + Math.Round(sum, 2) + " UAH");
        if (a.Category != "STD")
            Console.WriteLine("    (" + CategoryName(a.Category) + " not included)");
    }

    public static string CategoryName(string cat)
    {
        switch (cat)
        {
            case "VET": return "combat veteran -25%";
            case "CHN": return "Chornobyl -50%";
            case "LRG": return "large family -25%";
            case "STD": return "standard";
        }
        return cat;
    }
}
