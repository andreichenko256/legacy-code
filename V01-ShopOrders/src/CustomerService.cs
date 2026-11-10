namespace ShopOrders;

public class CustomerService
{
    // рівень за сумою покупок, без пониження
    public string UpdateLevel(Customer c)
    {
        string old = c.Level;
        if (c.TotalSpent >= 50000m)
            c.Level = "PLATINUM";
        else if (c.TotalSpent >= 20000m && c.Level != "PLATINUM")
            c.Level = "GOLD";
        else if (c.TotalSpent >= 5000m && (c.Level == "NONE" || c.Level == ""))
            c.Level = "SILVER";
        if (old != c.Level)
            Console.WriteLine("  customer " + c.Id + " level " + old + " -> " + c.Level);
        return c.Level;
    }

    // text for the account page
    public string DescribeBenefits(Customer cust)
    {
        decimal loyaltyPct = 0m;
        switch (cust.Level)
        {
            case "SILVER": loyaltyPct = 0.02m; break;
            case "GOLD": loyaltyPct = 0.04m; break;
            case "PLATINUM": loyaltyPct = 0.07m; break;
            default: loyaltyPct = 0m; break;
        }
        string s = cust.Name + " [" + cust.Level + "]: " + (int)(loyaltyPct * 100) + "% off";
        if (cust.Level == "NONE")
            s += ", spend " + (5000m - cust.TotalSpent).ToString("0.00") + " more for SILVER";
        else if (cust.Level == "SILVER")
            s += ", spend " + (20000m - cust.TotalSpent).ToString("0.00") + " more for GOLD";
        return s;
    }
}
