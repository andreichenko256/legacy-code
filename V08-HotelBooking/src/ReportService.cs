namespace HotelBooking;

// Invoices and reports for the front desk
public class ReportService
{
    private HotelRepository repo;

    public ReportService(HotelRepository r)
    {
        repo = r;
    }

    public static string TypeName(string t)
    {
        switch (t)
        {
            case "STD": return "Standard";
            case "DBL": return "Double";
            case "FAM": return "Family";
            case "LUX": return "Suite";
            default: return t;
        }
    }

    public void PrintInvoice(Booking b)
    {
        int a = b.Adults;
        int c = b.Children;
        bool bf = b.Breakfast;
        Room r = repo.GetRoom(b.RoomNo);
        double rate = r.Price;
        if (a > 2)
            rate = rate + (a - 2) * 450;
        if (c > 0)
            rate = rate + c * 250;
        if (bf)
            rate = rate + (a + c) * 180;
        double tax = a * b.Nights * 30;
        Console.WriteLine("  invoice #" + b.Id + " " + b.Guest.Name + ", " + TypeName(r.Type));
        Console.WriteLine("    " + b.Nights + " x " + rate + " + tourist tax " + tax);
        double disc = rate * b.Nights + tax - b.Total;
        if (disc > 0.01)
            Console.WriteLine("    discounts: -" + Math.Round(disc, 2));
        if (b.Status == BookingStatus.Cancelled)
            Console.WriteLine("    cancelled, fee " + b.Fee + " UAH");
        else
            Console.WriteLine("    total " + b.Total + " UAH, " + b.Status);
    }

    // заповненість на дату (для ранкової планерки)
    public void PrintOcc(DateTime day)
    {
        int busy = 0;
        int people = 0;
        foreach (Room r in repo.Rooms)
        {
            string who = "-";
            foreach (Booking b in repo.Bookings)
            {
                if (b.RoomNo != r.Number || b.Status == BookingStatus.Cancelled)
                    continue;
                if (day.Date >= b.CheckIn && day.Date < b.CheckOut())
                {
                    who = b.Guest.Name + " (#" + b.Id + ")";
                    busy++;
                    people = people + b.Adults + b.Children;
                }
            }
            if (r.OutOfService)
                who = "out of service";
            Console.WriteLine("  " + r.Number + " " + TypeName(r.Type) + ": " + who);
        }
        Console.WriteLine("  occupied " + busy + "/" + repo.Rooms.Count + " ("
            + busy * 100 / repo.Rooms.Count + "%), guests " + people);
    }
}
