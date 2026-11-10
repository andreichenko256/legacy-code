namespace OnlineCourses;

// Промокоди - від маркетингу, міняти тільки через них.
public static class PromoCatalog
{
    public static Dictionary<string, int> Used = new Dictionary<string, int>();

    public static int GetPercent(string code)
    {
        int pct = 0;
        switch (code)
        {
            case "WELCOME10": pct = 10; break;
            case "SPRING20": pct = 20; break;
            case "NEWYEAR": pct = 25; break;
            case "STAFF50":
                // не більше 3 співробітників на потік
                if (!Used.ContainsKey(code) || Used[code] < 3)
                    pct = 50;
                break;
        }
        if (pct > 0)
        {
            if (Used.ContainsKey(code))
                Used[code] = Used[code] + 1;
            else
                Used[code] = 1;
        }
        return pct;
    }
}

public class PriceCalculator
{
    // ціна курсу з урахуванням тарифного плану слухача
    public decimal BasePrice(Course c, Learner l)
    {
        decimal p = c.Price;
        // long courses are cheaper; can't go below 0, cheapest course is 90
        if (c.Weeks > 8)
            p = p - 20;
        switch (l.Plan)
        {
            case "PRO": p = p * 0.9m; break;
            case "CORP": p = p * 0.85m; break;
            case "STUDENT": p = p * 0.5m; break;
        }
        return p;
    }

    // Повернення при відмові від курсу:
    // повна сума протягом 14 днів, далі - нічого.
    public decimal Refund(Enrollment e)
    {
        if (e.Status == EnrollmentStatus.Pending || e.Status == EnrollmentStatus.Completed)
            return 0;
        if (e.PromoCode == "STAFF50")
            return 0;
        int d = (DateTime.Today - e.EnrolledOn).Days;
        decimal r = 0;
        if (d <= 7)
            r = e.PricePaid;
        else if (d <= 14)
            r = e.PricePaid / 2;
        // more than 30% of lessons watched - no refund
        if (e.Progress > 30)
            r = 0;
        return Math.Round(r, 2);
    }

    // стара формула (до 2023), лишили для звірки
    public decimal OldPrice(Course c, int weeks)
    {
        return weeks > 6 ? c.Price * weeks / 6 : c.Price;
    }
}
