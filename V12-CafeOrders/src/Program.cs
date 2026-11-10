namespace CafeOrders;

public class Program
{
    static CafeRepository repo = new CafeRepository();
    static OrderDesk desk = new OrderDesk(repo);
    static DateTime day = DateTime.Today;   // час - з паперових чеків

    public static void Main(string[] args)
    {
        Console.WriteLine("=== Kava & Bulka till, menu: " + repo.Menu.Count + " items ===");
        Console.WriteLine("-- orders");
        int a = Slip(3, 2, "Olha", null, 9.5, "CAP*2, CRO*2");
        int b = Slip(7, 4, "Taras", null, 13, "SND*2, CES, LEM*3");
        int c = Slip(12, 6, "Olha", null, 16.5, "LAT*6, CHC*3, TIR*3");
        int d = Slip(15, 8, "Taras", "student", 19, "CAP*4, CIN*3, TIR*2, ESP");
        int e = Slip(0, 1, null, "FREECOFFEE", 11, "CAP*2, CRO");
        int f = Slip(41, 1, null, null, 17, "LEM*2, AMR");
        int g = Slip(20, 10, "Olha", null, 13.5, "SND*4, GRK*3, QUI*3, LEM*5, TEA*5");
        int h = Slip(2, 1, "Taras", "STAFF", 15, "QUI, AMR*?");
        int k = Slip(5, 2, "Olha", null, 10, "AMR, CIN");
        Console.WriteLine("-- rejected");
        Slip(77, 2, "Olha", null, 12, "TEA");
        Slip(6, 0, "Taras", null, 12, "TEA");
        Check("#" + a + " add XYZ", desk.AddItem(a, "XYZ", 1));
        Check("#99 add LAT", desk.AddItem(99, "LAT", 1));
        Check("#" + f + " add MAT", desk.AddItem(f, "MAT", 1));
        Console.WriteLine("-- status");
        Pay(c, "CARD", 0, false);       // ще не подали
        foreach (int id in new int[] { a, b, c, d, f, g, h })
            desk.ChangeStatus(id, OrderStatus.Served);
        Step(k, OrderStatus.Cancelled);
        Step(99, OrderStatus.Served);
        Console.WriteLine("-- bills");
        new ShiftReport().PrintBill(repo.FindOrder(d), new MenuService(repo), "CARD");
        Settle(new int[] { a, b, c, d, e, f, g, h, k });
    }

    // замовлення "з паперу": час беремо з чека
    static int Slip(int table, int guests, string waiter, string coupon, double hour, string items)
    {
        int id = desk.Open(table, guests, table == 0, waiter, coupon);
        if (id < 0)
        {
            Console.WriteLine("  open failed: " + desk.LastError);
            return id;
        }
        Order o = repo.FindOrder(id);
        o.CreatedAt = day.AddHours(hour);
        desk.addFromSlip(id, items);
        Console.WriteLine("  " + desk.Describe(o));
        return id;
    }

    static void Check(string what, bool ok)
    {
        Console.WriteLine("  " + what + ": " + (ok ? "ok" : "refused (" + desk.LastError + ")"));
    }

    static void Step(int id, OrderStatus to)
    {
        bool ok = desk.ChangeStatus(id, to);
        Console.WriteLine("  #" + id + " -> " + to + ": " + (ok ? "ok" : "refused"));
    }

    static void Pay(int id, string payment, decimal tip, bool noService)
    {
        decimal sum = desk.Pay(id, payment, tip, noService);
        if (sum < 0)
            Console.WriteLine("  pay #" + id + ": " + sum + " (" + desk.LastError + ")");
        else
            Console.WriteLine("  pay #" + id + " " + payment + ": " + sum.ToString("0.00"));
    }

    // розрахунок столів (стіл 7 ще сидить)
    static void Settle(int[] t)
    {
        Console.WriteLine("-- payments");
        Pay(t[0], "CARD", 20, false);
        Pay(t[2], "CARD", 50, false);
        Pay(t[3], "CASH", 0, false);
        Pay(t[4], "CASH", 0, false);
        Pay(t[5], "CARD", 0, false);
        Pay(t[6], "CARD", 0, true);     // скарга: сервіс знято
        Pay(t[7], "CARD", 0, false);
        Pay(t[8], "CARD", 0, false);
        Console.WriteLine("-- after payment");
        Check("#" + t[0] + " add LAT", desk.AddItem(t[0], "LAT", 1));
        Console.WriteLine("  #" + t[0] + " repeat round: +" + desk.RepeatRound(t[0]) + " line(s)");
        Order o = repo.FindOrder(t[0]);
        Console.WriteLine("  " + desk.Describe(o) + ", paid " + o.PaidAmount.ToString("0.00"));
        Step(t[0], OrderStatus.Served);
        Console.WriteLine("-- shift");
        new ShiftReport().PrintShift(repo.Orders);
    }
}
