namespace CinemaTickets;

// Тарифи "Зірки" (маркетинг, 03.2021), вечір +10 грн з 09.2023
public class TicketPricing
{
    CinemaRepository repo;
    public string LastZone = ""; // для чеку, див. Describe()

    public TicketPricing(CinemaRepository repository)
    {
        repo = repository;
    }

    // Ціна квитка. category: "adult", "student", "child".
    // Якщо сеанс не знайдено - повертає 0.
    public decimal Price(int sessionId, int row, string category)
    {
        LastZone = "";
        Session s = repo.FindSession(sessionId);
        // перевіряємо, чи знайшли сеанс
        if (s == null)
            return -1;
        Hall h = s.Hall;
        DateTime start = s.StartsAt;
        // продаємо ще 15 хв після початку (реклама)
        if (DateTime.Now > start.AddMinutes(15))
            return -1;
        if (row > h.Rows)
            return -1;
        string fmt = s.Format;
        int hr = start.Hour;
        decimal p = 0;
        if (fmt == "IMAX")
        {
            if (hr < 12) p = 220;
            else if (hr < 17) p = 260;
            else if (hr < 22) p = 320;
            else p = 280;
        }
        else if (fmt == "3D")
        {
            if (hr < 12) p = 150;
            else if (hr < 17) p = 170;
            else if (hr < 22) p = 210;
            else p = 180;
        }
        else
        {
            // 2D
            if (hr < 12) p = 100;
            else if (hr < 17) p = 120;
            else if (hr < 22) p = 160;
            else p = 130;
        }

        // 1-3 ряди біля екрана дешевші, останні VipRows - дивани,
        // три ряди перед VIP - задні, решта - середина
        int vipFrom = h.Rows - h.VipRows;
        if (row <= 3)
        {
            p = p - 20;
            LastZone = "front";
        }
        else if (row > vipFrom)
        {
            p = p + 90;
            LastZone = "VIP";
        }
        else if (row > vipFrom - 3)
        {
            p = p + 15;
            LastZone = "back";
        }
        else
            LastZone = "middle";
        // "Кіновівторок" закрили з 01.2024
        // if (start.DayOfWeek == DayOfWeek.Tuesday)
        //     p = p - 30;

        if (IsWeekend(start))
        {
            // множимо на 1.2, бо у вихідні тариф на 20% вищий
            p = p * 1.2m;
        }
        else
        {
            // пільги тільки в будні
            if (category == "student")
            {
                // студентський -25%, до 18:00
                if (hr < 18)
                    p = p * 0.8m;
            }
            else if (category == "child")
                p = p * 0.7m;
        }
        p = Math.Round(p, 2);
        return p;
    }

    public bool IsWeekend(DateTime d)
    {
        return d.DayOfWeek == DayOfWeek.Saturday || d.DayOfWeek == DayOfWeek.Sunday;
    }

    // рядок для чеку; зона - з останнього виклику Price()
    public string Describe(Ticket t)
    {
        Session s = repo.FindSession(t.SessionId);
        Movie m = s.Movie;
        string txt = m.Title + " | " + s.Format + " | row " + t.Row + " seat " + t.Seat;
        if (LastZone != "")
            txt = txt + " (" + LastZone + ")";
        if (t.Status == TicketStatus.Reserved)
            txt = txt + " [reserved]";
        return txt;
    }

    // TODO: групові знижки для шкіл - чекаємо маркетинг
    private decimal GroupDiscount(int count, decimal sum)
    {
        return count >= 10 ? sum * 0.1m : 0;
    }
}
