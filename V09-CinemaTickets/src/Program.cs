namespace CinemaTickets;

public class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine("=== Zirka Cinema: box office demo ===");
        CinemaRepository repo = Seed();
        foreach (Session s in repo.Sessions)
            Console.WriteLine("Session " + s.Id + ": " + s.Movie.Title + ", " + s.Format
                + ", hall " + s.Hall.Name);
        Session s6 = repo.AddSession(6, repo.Movies[0], repo.Halls[0],
            NextDay(DayOfWeek.Wednesday, 20, 0), "2D");
        Console.WriteLine("Session 6 overlaps session 2: " + (s6 == null ? "rejected" : "added"));
        TicketPricing pricing = new TicketPricing(repo);
        Console.WriteLine("-- Prices (sessions 3 and 5 are on weekend) --");
        ShowPrice(pricing, 1, 5, "adult");
        ShowPrice(pricing, 1, 2, "student");
        ShowPrice(pricing, 1, 0, "adult");
        ShowPrice(pricing, 1, 13, "adult");
        ShowPrice(pricing, 2, 11, "adult");
        ShowPrice(pricing, 2, 6, "student");
        ShowPrice(pricing, 4, 3, "adult");
        ShowPrice(pricing, 3, 14, "adult");
        ShowPrice(pricing, 3, 12, "child");
        ShowPrice(pricing, 5, 5, "adult");
        ShowPrice(pricing, 42, 5, "adult");
        DemoBoxOffice(repo);
    }

    static void DemoBoxOffice(CinemaRepository repo)
    {
        BoxOffice box = new BoxOffice(repo);
        Console.WriteLine("-- Box office --");
        Ticket t1 = box.Sell(2, 6, 7, "adult", "Olena Koval", "+380671234567", false);
        box.Pay(t1.Number);
        box.Admit(t1.Number);
        box.Refund(t1.Number);
        Ticket t2 = box.Sell(3, 8, 10, "student", "Taras Melnyk", null, true);
        box.Refund(t2.Number);
        Ticket t3 = box.Sell(1, 4, 3, "child", "Iryna Bondar", "+380501112233", false);
        box.Admit(t3.Number);
        box.Sell(1, 4, 3, "adult", "Petro Savchuk", null, true);
        box.Sell(2, 3, 0, "adult", "Petro Savchuk", null, true);
        box.Sell(4, 5, 5, "child", "Iryna Bondar", null, true);
        box.Sell(77, 1, 1, "adult", "Andrii Lysenko", null, true);
        box.Pay("CT-99999");
        Ticket t4 = box.Sell(3, 5, 1, "adult", "Maria Shevchuk", "+380931234567", false);
        t4.ReservedAt = DateTime.Now.AddMinutes(-45); // ніби бронь 45 хв тому
        Console.WriteLine("Expired reservations: " + box.ExpireReservations());
        box.Pay(t4.Number);
        Console.WriteLine("-- Cash report --");
        box.print_cash(2);
    }

    static CinemaRepository Seed()
    {
        CinemaRepository repo = new CinemaRepository();
        Movie m1 = new Movie { Id = 1, Title = "The Last Orbit", Minutes = 128, AgeLimit = 12 };
        Movie m2 = new Movie { Id = 2, Title = "Paper Lanterns", Minutes = 96 };
        Movie m3 = new Movie { Id = 3, Title = "Night Train", Minutes = 141, AgeLimit = 16 };
        repo.Movies.AddRange(new[] { m1, m2, m3 });
        Hall red = new Hall { Id = 1, Name = "Red", Rows = 12, SeatsPerRow = 14, VipRows = 2 };
        Hall imax = new Hall { Id = 2, Name = "IMAX", Rows = 16, SeatsPerRow = 22, VipRows = 3 };
        imax.Imax = true;
        Hall blue = new Hall { Id = 3, Name = "Blue", Rows = 8, SeatsPerRow = 10 };
        repo.Halls.AddRange(new[] { red, imax, blue });
        repo.AddSession(1, m1, red, NextDay(DayOfWeek.Wednesday, 10, 30), "2D");
        repo.AddSession(2, m2, red, NextDay(DayOfWeek.Wednesday, 19, 0), "3D");
        repo.AddSession(3, m1, imax, NextDay(DayOfWeek.Saturday, 20, 0), "IMAX");
        repo.AddSession(4, m3, blue, NextDay(DayOfWeek.Thursday, 23, 15), "2D");
        repo.AddSession(5, m2, blue, NextDay(DayOfWeek.Sunday, 13, 0), "3d");
        return repo;
    }

    // найближчий такий день тижня після сьогодні
    static DateTime NextDay(DayOfWeek day, int hour, int minute)
    {
        DateTime d = DateTime.Today.AddDays(1);
        while (d.DayOfWeek != day)
            d = d.AddDays(1);
        return d.AddHours(hour).AddMinutes(minute);
    }

    static void ShowPrice(TicketPricing pricing, int sessionId, int row, string category)
    {
        decimal v = pricing.Price(sessionId, row, category);
        Console.WriteLine("  session " + sessionId + ", row " + row + ", " + category + ": "
            + (v < 0 ? "n/a" : v.ToString("0.00")));
    }
}
