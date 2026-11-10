namespace CinemaTickets;

// Каса: продаж, бронь, оплата, вхід у зал, повернення.
// Писали у 2019 під один зал, далі дописували.
public class BoxOffice
{
    public CinemaRepository repo;
    public TicketPricing pricing;
    public string logFile = Path.Combine(Path.GetTempPath(), "zirka_boxoffice.log");
    public int sold;
    public int refunds;
    public decimal cash;
    public decimal refundedSum;
    public string lastError = "";
    public List<string> shiftLog = new List<string>();
    public int reserveMinutes = 30;
    public static int soldAllShifts = 0;
    public static bool useLoyaltyCards = false;
    string curCustomer; // тільки на час Sell()
    string curPhone;

    public BoxOffice(CinemaRepository r)
    {
        repo = r;
        pricing = new TicketPricing(r);
    }

    // payNow: true = sale, false = reservation for 30 min
    public Ticket Sell(int sessionId, int row, int seat, string category, string customer,
        string phone, bool payNow)
    {
        curCustomer = customer;
        curPhone = phone;
        Session s = repo.FindSession(sessionId);
        if (s == null)
        {
            lastError = "Session " + sessionId + " not found";
            Console.WriteLine("ERROR: " + lastError);
            return null;
        }
        if (!SeatExists(s.Hall, row, seat) || repo.isSeatTaken(sessionId, row, seat))
        {
            lastError = "Seat " + row + "/" + seat + " is not available";
            Console.WriteLine("ERROR: " + lastError);
            return null;
        }
        // дітям тільки 0+ і 12+
        if (category == "child" && s.Movie.AgeLimit >= 16)
        {
            lastError = "Child ticket is not allowed for " + s.Movie.AgeLimit + "+ movie";
            Console.WriteLine("ERROR: " + lastError);
            return null;
        }
        decimal price = pricing.Price(sessionId, row, category);
        if (price < 0)
            return null; // продаж закрито
        if (s.Format == "3D")
            price = price + 25; // окуляри
        if (useLoyaltyCards) // вимкнули після переходу на нову CRM
            price = price - price * 5 / 100;
        Ticket t = repo.NewTicket(sessionId, row, seat);
        t.Category = category;
        t.Customer = curCustomer;
        t.Phone = curPhone;
        t.Price = price;
        WriteLog((payNow ? "SELL " : "RESERVE ") + t.Number + " " + price);
        if (payNow)
        {
            // статус напряму, ChangeStatus тут не підходить
            t.Status = TicketStatus.Paid;
            sold++;
            soldAllShifts++;
            cash += t.Price;
            WriteLog("PAY " + t.Number);
            Console.WriteLine("Paid " + t.Number + ": " + t.Price.ToString("0.00"));
        }
        Console.WriteLine((payNow ? "Sold " : "Reserved ") + t.Number + ": "
            + pricing.Describe(t) + " = " + price.ToString("0.00"));
        return t;
    }

    public bool Pay(string number)
    {
        Ticket t = repo.FindTicket(number);
        if (t == null)
        {
            lastError = "Ticket " + number + " not found";
            Console.WriteLine("ERROR: " + lastError);
            WriteLog("ERR " + lastError);
            return false;
        }
        if (t.Status != TicketStatus.Reserved)
        {
            lastError = "Cannot pay ticket " + number + " in status " + t.Status;
            Console.WriteLine("ERROR: " + lastError);
            return false;
        }
        t.Status = TicketStatus.Paid;
        sold++;
        soldAllShifts++;
        cash += t.Price;
        WriteLog("PAY " + t.Number);
        Console.WriteLine("Paid " + t.Number + ": " + t.Price.ToString("0.00"));
        return true;
    }

    // контроль на вході в зал
    public bool Admit(string number)
    {
        Ticket t = repo.FindTicket(number);
        if (t == null)
        {
            lastError = "Ticket " + number + " not found";
            Console.WriteLine("ERROR: " + lastError);
            WriteLog("ERR " + lastError);
            return false;
        }
        Session s = repo.FindSession(t.SessionId);
        if (DateTime.Now > s.EndsAt() || !t.ChangeStatus(TicketStatus.Used))
        {
            lastError = "Cannot admit " + number + " in status " + t.Status;
            Console.WriteLine("ERROR: " + lastError);
            return false;
        }
        WriteLog("ADMIT " + number);
        Console.WriteLine("Admitted " + number + ", hall " + s.Hall.Name);
        return true;
    }

    // Повертаємо 90% (10% - касовий збір), у день сеансу - 50%.
    // Пізніше ніж за 30 хв до початку - не повертаємо.
    public bool Refund(string number)
    {
        Ticket t = repo.FindTicket(number);
        if (t == null)
        {
            lastError = "Ticket " + number + " not found";
            Console.WriteLine("ERROR: " + lastError);
            WriteLog("ERR " + lastError);
            return false;
        }
        if (t.Status == TicketStatus.Used)
        {
            lastError = "Ticket " + number + " is already used, no refund";
            Console.WriteLine("ERROR: " + lastError);
            return false;
        }
        Session s = repo.FindSession(t.SessionId);
        decimal back;
        if ((s.StartsAt.Date - DateTime.Today).Days >= 1)
            back = (int)(t.Price * 0.9m);
        else
            back = (int)(t.Price * 0.5m);
        if (DateTime.Now > s.StartsAt.AddMinutes(-30) || !t.ChangeStatus(TicketStatus.Refunded))
        {
            lastError = "Cannot refund " + number + " in status " + t.Status;
            Console.WriteLine("ERROR: " + lastError);
            return false;
        }
        refunds++;
        refundedSum += back;
        cash -= back;
        WriteLog("REFUND " + number + " " + back);
        Console.WriteLine("Refunded " + number + ": " + back.ToString("0.00") + " of "
            + t.Price.ToString("0.00"));
        return true;
    }

    // expires reservations older than 30 min and those less than 30 min before the show
    public int ExpireReservations()
    {
        int n = 0;
        foreach (Ticket t in repo.Tickets)
        {
            if (t.Status != TicketStatus.Reserved)
                continue;
            Session s = repo.FindSession(t.SessionId);
            if ((DateTime.Now - t.ReservedAt).TotalMinutes > reserveMinutes
                || s.StartsAt.AddMinutes(-30) < DateTime.Now)
            {
                t.ChangeStatus(TicketStatus.Expired);
                n++;
                WriteLog("EXPIRED " + t.Number);
                Console.WriteLine("Reservation " + t.Number + " expired");
            }
        }
        return n;
    }

    public bool SeatExists(Hall h, int row, int seat)
    {
        if (row < 1 || seat < 1)
            return false;
        return row <= h.Rows && seat <= h.SeatsPerRow;
    }

    // каса по сеансу + підсумок зміни (для інкасації)
    public void print_cash(int sessionId)
    {
        Session s = repo.FindSession(sessionId);
        if (s == null)
            return;
        int sold = 0;
        decimal sum = 0;
        int reserved = 0;
        foreach (Ticket t in repo.Tickets)
        {
            if (t.SessionId != sessionId)
                continue;
            if (t.Status == TicketStatus.Paid || t.Status == TicketStatus.Used)
            {
                sold++;
                sum += t.Price;
            }
            else if (t.Status == TicketStatus.Reserved)
                reserved++;
        }
        int seats = s.Hall.Rows * s.Hall.SeatsPerRow;
        Console.WriteLine("Session " + sessionId + " [" + s.Format + "]: sold " + sold + "/"
            + seats + ", reserved " + reserved + ", cash " + sum.ToString("0.00"));
        // менше 20% залу - сигнал програмному відділу
        if (sold * 100 / seats < 20)
            Console.WriteLine("  low occupancy");
        Console.WriteLine("Shift: sold " + this.sold + ", refunds " + refunds + " ("
            + refundedSum.ToString("0.00") + "), cash " + cash.ToString("0.00"));
    }

    void WriteLog(string msg)
    {
        shiftLog.Add(msg);
        try { File.AppendAllText(logFile, DateTime.Now.ToString("s") + " " + msg + "\n"); }
        catch { }
    }
}
