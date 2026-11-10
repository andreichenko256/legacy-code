namespace HotelBooking;

public class BookingService
{
    private HotelRepository repo;
    private PriceCalculator calc = new PriceCalculator();
    public string LastError;         // текст помилки останнього Reserve

    public BookingService(HotelRepository repo)
    {
        this.repo = repo;
    }

    // Повертає Id бронювання або -1 (причина - в LastError)
    public int Reserve(string gn, string ph, DateTime d1, int n, int a, int c,
        string rt, bool bf, string promo)
    {
        LastError = null;
        if (gn == null || gn.Trim() == "")
        {
            LastError = "guest name is empty";
            return -1;
        }
        if (n <= 0 || a <= 0)
        {
            LastError = "nights and adults must be positive";
            return -1;
        }
        if (d1.Date < DateTime.Today)
        {
            LastError = "check-in date is in the past";
            return -1;
        }
        // з 22:00 нічний аудит, на сьогодні вже не продаємо
        if (d1.Date == DateTime.Today && DateTime.Now.Hour >= 22)
        {
            LastError = "same-day booking is closed after 22:00";
            return -1;
        }
        string p = ph.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");
        if (p.StartsWith("0"))
            p = "+38" + p;
        Guest g = repo.FindGuest(p);
        if (g == null)
        {
            g = new Guest();
            g.Name = gn.Trim();
            g.Phone = p;
            repo.Guests.Add(g);
        }
        else if (g.Blacklisted)
        {
            Console.WriteLine("  WARNING: blacklisted guest " + g.Name + " tried to book");
            LastError = "guest is blacklisted";
            return -3;
        }

        // старий сайт шле "standard", "std", "" - незнайоме = STD
        // TODO: тимчасово, поки сайт не перейде на нові коди
        string t = rt.Trim().ToUpper();
        if (t != "DBL" && t != "FAM" && t != "LUX")
            t = "STD";
        Room room = null;
        foreach (Room r in repo.Rooms)
        {
            if (r.Type == t && !r.OutOfService)
            {
                if (chk_fit(r, a, c))
                {
                    // перевіряємо, чи номер вільний на ці дати
                    if (!repo.isBusy(r.Number, d1, n, 0))
                    {
                        room = r;
                        break;
                    }
                }
            }
        }
        if (room == null)
        {
            LastError = "no free " + t + " room for " + a + "+" + c;
            return -2;
        }

        double total = calc.calcTotal(room, d1, n, a, c, bf, promo);
        // 0.95 - постійним гостям (3+ проживання) ще 5%
        if (g.Visits >= 3)
            total = total * 0.95;
        Booking b = new Booking();
        b.Id = HotelRepository.nextId++;
        b.Guest = g;
        b.RoomNo = room.Number;
        b.RoomType = room.Type;
        b.CheckIn = d1.Date;
        b.Nights = n;
        b.Adults = a;
        b.Children = c;
        b.Breakfast = bf;
        b.Promo = promo;
        b.Total = Math.Round(total, 2);
        b.Created = DateTime.Now;
        repo.Bookings.Add(b);
        //g.Visits++;   рахуємо при виїзді, див. ChangeStatus
        AuditLog.Write("reserve #" + b.Id + " room " + room.Number + " " + g.Phone);
        return b.Id;
    }

    // дорослі - на ліжка, ще одна дитина - на розкладачку
    public bool chk_fit(Room r, int a, int c)
    {
        if (a > r.Beds)
            return false;
        return a + c <= r.Beds + 1;
    }

    // гість у картотеку без бронювання (лист очікування)
    public Guest RegisterGuest(string gn, string ph)
    {
        if (gn == null || gn.Trim() == "")
            return null;
        string p = ph.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");
        if (p.StartsWith("0"))
            p = "+38" + p;
        Guest g = repo.FindGuest(p);
        if (g == null)
        {
            g = new Guest();
            g.Name = gn.Trim();
            g.Phone = p;
            repo.Guests.Add(g);
        }
        return g;
    }

    // false - не знайдено або перехід не дозволений
    public bool ChangeStatus(int id, BookingStatus to)
    {
        Booking b = repo.GetBooking(id);
        if (b == null)
            return false;
        bool ok = false;     // CheckedOut, Cancelled - кінцеві
        switch (b.Status)
        {
            case BookingStatus.Pending:
                ok = to == BookingStatus.Confirmed || to == BookingStatus.Cancelled;
                break;
            case BookingStatus.Confirmed:
                ok = to == BookingStatus.CheckedIn || to == BookingStatus.Cancelled;
                break;
            case BookingStatus.CheckedIn:
                ok = to == BookingStatus.CheckedOut;
                break;
        }
        if (!ok)
        {
            Console.WriteLine("  ! #" + id + ": " + b.Status + " -> " + to + " not allowed");
            return false;
        }
        if (to == BookingStatus.Cancelled)
            b.Fee = calc.CancelFee(b);
        if (to == BookingStatus.CheckedOut)
            b.Guest.Visits++;
        b.Status = to;
        AuditLog.Write("status #" + id + " " + to);
        return true;
    }

    // extend the stay; returns extra charge or -1
    public double ExtendStay(int id, int extra)
    {
        Booking b = repo.GetBooking(id);
        if (b == null || extra <= 0)
            return -1;
        if (b.Status == BookingStatus.Cancelled || b.Status == BookingStatus.CheckedOut)
            return -1;
        if (repo.isBusy(b.RoomNo, b.CheckOut(), extra, b.Id))
            return -1;
        Room r = repo.GetRoom(b.RoomNo);
        int a = b.Adults;
        int c = b.Children;
        bool bf = b.Breakfast;
        double rate = r.Price;
        if (a > 2)
            rate = rate + (a - 2) * 450;
        if (c > 0)
            rate = rate + c * 250;
        if (bf)
            rate = rate + (a + c) * 180;
        // знижки на продовження не діють, тільки збір
        double add = rate * extra + a * extra * 30;
        b.Nights = b.Nights + extra;
        b.Total = b.Total + add;
        AuditLog.Write("extend #" + id + " +" + extra);
        return add;
    }
}
