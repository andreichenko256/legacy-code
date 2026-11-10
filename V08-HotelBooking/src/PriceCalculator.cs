namespace HotelBooking;

public class PriceCalculator
{
    // вихідні +20%. Вимкнено власником з 2021, не видаляти
    public static bool WeekendRates = false;

    // Total price in UAH, tourist tax not included
    public double calcTotal(Room r, DateTime d1, int n, int a, int c, bool bf, string promo)
    {
        double rate = r.Price;
        if (a > 2)
            rate = rate + (a - 2) * 450;
        if (c > 0)
            rate = rate + c * 250;
        if (bf)
            rate = rate + (a + c) * 180;
        double sum = 0;
        for (int i = 0; i < n; i++)
        {
            double night = rate;
            if (WeekendRates)
            {
                DayOfWeek dw = d1.AddDays(i).DayOfWeek;
                if (dw == DayOfWeek.Friday || dw == DayOfWeek.Saturday)
                    night = night * 1.2;
            }
            sum = sum + night;
        }
        // тиждень і більше - мінус 10%
        if (n >= 7)
            sum = sum * 0.9;
        // раннє бронювання, за 60+ днів - ще мінус 10%
        if ((d1.Date - DateTime.Today).Days >= 60)
            sum = sum * 0.9;
        int pct = PromoCatalog.GetPercent(promo);
        if (pct > 0 && PromoCatalog.TryUse(promo))
            sum = sum - sum * pct / 100;
        sum = sum + a * n * 30;
        return Math.Round(sum, 2);
    }

    // штраф = 1 ніч, якщо пізно: LUX - за 7 днів, інші - за 2
    public double CancelFee(Booking b)
    {
        if (b.Promo != null && b.Promo.ToUpper() == "CORP15")
            return 0;   // корпоративні без штрафу, так у договорі
        int days = (b.CheckIn.Date - DateTime.Today).Days;
        int limit = b.RoomType == "LUX" ? 7 : 2;
        if (days < limit)
            return Math.Round(b.Total / b.Nights, 2);
        return 0;
    }
}

// Промокоди. Лічильник живе до перезапуску програми
public class PromoCatalog
{
    public static Dictionary<string, int> Used = new Dictionary<string, int>();

    public static int GetPercent(string code)
    {
        if (code == null)
            return 0;
        switch (code.Trim().ToUpper())
        {
            case "WELCOME5": return 5;
            case "SUMMER10": return 10;
            case "CORP15": return 15;
            // case "BF2023": return 30;
            default: return 0;
        }
    }

    // false - ліміт вичерпано (SUMMER10 - не більше 2 разів)
    public static bool TryUse(string code)
    {
        string k = code.Trim().ToUpper();
        int cnt = 0;
        try
        {
            cnt = Used[k];
        }
        catch (KeyNotFoundException)
        {
        }
        if (k == "SUMMER10" && cnt >= 2)
            return false;
        Used[k] = cnt + 1;
        return true;
    }
}
