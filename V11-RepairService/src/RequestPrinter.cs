namespace RepairService;

// Друк карток і звітів
public class RequestPrinter
{
    private int reportCount;     // потрібні тільки під час звіту
    private decimal reportSum;

    public string StatusText(string st)
    {
        switch (st)
        {
            case Statuses.New: return "Accepted";
            case Statuses.Diagnosed: return "Diagnosed";
            case Statuses.WaitingParts: return "Waiting for parts";
            case Statuses.InRepair: return "In repair";
            case Statuses.Ready: return "Ready for pickup";
            case Statuses.Delivered: return "Delivered to customer";
            case Statuses.Cancelled: return "Cancelled";
            case Statuses.Warranty: return "Warranty repair";
            default: return "Unknown (" + st + ")";
        }
    }

    public void PrintCard(RepairRequest r)
    {
        if (r == null)
            return;
        Console.WriteLine("  #" + r.Id + " " + r.DeviceType + " " + r.Model + " - "
            + StatusText(r.Status));
        string who = r.Customer == null ? "?" : r.Customer.Name;
        Console.WriteLine("    customer: " + who + ", technician: " + (r.Technician ?? "-"));
        if (r.Total > 0)
            Console.WriteLine("    total " + r.Total.ToString("0.00") + ", paid "
                + r.Paid.ToString("0.00") + ", debt " + (r.Total - r.Paid).ToString("0.00"));
        else if (r.Estimate > 0)
            Console.WriteLine("    estimate " + r.Estimate.ToString("0.00"));
        if (r.IsWarranty)
            Console.WriteLine("    warranty repair, no charge");
    }

    // daily report for the manager
    public void PrintDailyReport(List<RepairRequest> list)
    {
        Console.WriteLine("=== Daily report ===");
        reportCount = 0;
        reportSum = 0;
        decimal debt = 0;
        foreach (RepairRequest r in list)
        {
            if (r.Status == "Cancelled")
                continue;
            reportCount++;
            reportSum = reportSum + r.Total;
            debt = debt + (r.Total - r.Paid);
            Console.WriteLine("  #" + r.Id + " " + StatusText(r.Status) + ": "
                + r.Total.ToString("0.00"));
        }
        print_totals(debt);
    }

    // те саме по одному майстру (для премії)
    public void PrintTechReport(List<RepairRequest> all, string tech)
    {
        Console.WriteLine("=== Technician: " + tech + " ===");
        List<RepairRequest> list = new List<RepairRequest>();
        foreach (RepairRequest x in all)
            if (x.Technician == tech)
                list.Add(x);
        reportCount = 0;
        reportSum = 0;
        decimal debt = 0;
        foreach (RepairRequest r in list)
        {
            if (r.Status == "Cancelled")
                continue;
            reportCount++;
            reportSum = reportSum + r.Total;
            debt = debt + (r.Total - r.Paid);
            Console.WriteLine("  #" + r.Id + " " + StatusText(r.Status) + ": "
                + r.Total.ToString("0.00"));
        }
        print_totals(debt);
    }

    private void print_totals(decimal debt)
    {
        Console.WriteLine("  count: " + reportCount + ", sum: " + reportSum.ToString("0.00")
            + ", unpaid: " + debt.ToString("0.00"));
        if (reportCount > 0)
            Console.WriteLine("  average check: " + (reportSum / reportCount).ToString("0.00"));
    }
}
