namespace HotelBooking;

public class Program
{
    static HotelRepository repo = new HotelRepository();
    static BookingService svc = new BookingService(repo);

    public static void Main(string[] args)
    {
        DateTime d = DateTime.Today;    // дати демо - від сьогодні
        Console.WriteLine("=== Dnipro View Hotel - front desk ===");
        Console.WriteLine("rooms: " + repo.Rooms.Count + ", guests: " + repo.Guests.Count);
        Console.WriteLine("-- reservations");
        int b1 = Show(svc.Reserve("Olena Kovalenko", "067 321-45-67", d.AddDays(3), 3, 2, 0,
            "STD", true, null));
        int b2 = Show(svc.Reserve("Andrii Shevchuk", "(050) 765-43-21", d.AddDays(10), 7, 2, 1,
            "dbl", false, "SUMMER10"));
        int b3 = Show(svc.Reserve("Mariia Tkachenko", "0931112244", d.AddDays(75), 4, 2, 2,
            "FAM", true, "CORP15"));
        int b4 = Show(svc.Reserve("Iryna Melnyk", "+380 67 123 45 67", d.AddDays(1), 2, 1, 0,
            "LUX", false, null));
        Show(svc.Reserve("Taras Hnatiuk", "067-555-00-11", d.AddDays(20), 2, 2, 0,
            "Suite", false, null));
        int b6 = Show(svc.Reserve("Serhii Lysenko", "063 777 88 99", d.AddDays(6), 1, 1, 0,
            "STD", false, null));
        Show(svc.Reserve("Kateryna Bondar", "099 444 55 66", d.AddDays(14), 2, 2, -1,
            "STD", true, null));
        Console.WriteLine("-- rejected requests");
        Show(svc.Reserve("", "050 000 00 00", d.AddDays(5), 2, 1, 0, "STD", false, null));
        Show(svc.Reserve("Oleh Savchuk", "050 123 00 00", d.AddDays(-2), 2, 1, 0,
            "STD", false, null));
        Show(svc.Reserve("Petro Bondar", "050-111-22-33", d.AddDays(5), 2, 1, 0,
            "DBL", false, null));
        Show(svc.Reserve("Nadiia Koval", "068 222 33 44", d.AddDays(2), 2, 2, 0,
            "LUX", false, null));
        Guest w = svc.RegisterGuest("Oksana Rudenko", "097 000-11-22");
        Console.WriteLine("  waiting list: " + w.Name + " " + w.Phone + ", guests: "
            + repo.Guests.Count);
        Statuses(b1, b2, b4);
        Reports(b2, b3, b4, b6);
    }

    static int Show(int id)
    {
        if (id > 0)
            Console.WriteLine("  ok    " + repo.GetBooking(id).Summary());
        else
            Console.WriteLine("  error " + id + ": " + svc.LastError);
        return id;
    }

    static void Step(int id, BookingStatus to)
    {
        bool ok = svc.ChangeStatus(id, to);
        Console.WriteLine("  #" + id + " -> " + to + ": " + (ok ? "ok" : "refused"));
    }

    static void Statuses(int b1, int b2, int b4)
    {
        Console.WriteLine("-- status changes");
        Step(b1, BookingStatus.Confirmed);
        Step(b1, BookingStatus.CheckedIn);
        Step(b1, BookingStatus.Cancelled);     // вже заселився
        Step(b2, BookingStatus.Cancelled);
        Step(b2, BookingStatus.CheckedIn);     // скасоване не заселяємо
        Step(b4, BookingStatus.Cancelled);     // LUX за день до заїзду - штраф
        Step(b1, BookingStatus.CheckedOut);
        Step(99, BookingStatus.Confirmed);
        Console.WriteLine("  fees: " + repo.GetBooking(b2).Fee + ", " + repo.GetBooking(b4).Fee);
    }

    static void Reports(int b2, int b3, int b4, int b6)
    {
        ReportService rep = new ReportService(repo);
        Console.WriteLine("-- extend stay");
        Console.WriteLine("  #" + b3 + " +2 nights: " + svc.ExtendStay(b3, 2));
        Console.WriteLine("  #" + b2 + " +1 night (cancelled): " + svc.ExtendStay(b2, 1));
        Console.WriteLine("  #" + b6 + " +0 nights: " + svc.ExtendStay(b6, 0));
        Console.WriteLine("  #42 +1 night: " + svc.ExtendStay(42, 1));
        Console.WriteLine("-- invoices");
        rep.PrintInvoice(repo.GetBooking(b3));
        rep.PrintInvoice(repo.GetBooking(b4));
        Console.WriteLine("-- occupancy in 6 days");
        rep.PrintOcc(DateTime.Today.AddDays(6));
    }
}
