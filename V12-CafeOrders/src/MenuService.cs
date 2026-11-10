namespace CafeOrders;

public class MenuService
{
    private CafeRepository repo;
    public decimal LastDiscount;    // для чека (ShiftReport)
    public int LastServicePct;

    public MenuService(CafeRepository repo)
    {
        this.repo = repo;
    }

    // Рахунок до сплати, без чайових. payment: "CASH" / "CARD"
    public decimal Total(Order order, string payment, bool waiveService)
    {
        if (order.Status == OrderStatus.Cancelled)
            return 0;
        decimal sum = 0, disc = 0;
        int mains = 0, drinks = 0, cnt = 0;
        string soup = SoupOfDay();
        foreach (OrderItem it in order.Items)
        {
            if (it.Qty <= 0)
                continue;
            decimal line = it.Price * it.Qty;
            sum += line;
            cnt += it.Qty;
            if (it.Category == "DRINK")
            {
                drinks += it.Qty;
                // happy hour з 16:00 до 18:00
                if (order.CreatedAt.Hour >= 16 && order.CreatedAt.Hour < 18)
                {
                    // навинос не діє
                    if (!order.IsTakeaway)
                        disc += line * 0.2m;
                }
            }
            else if (it.Category == "MAIN")
                mains += it.Qty;
            // 0.85 - суп дня зі знижкою 15%
            if (it.Code == soup)
                disc += line * 0.15m;
        }
        // ланч-комбо 12-16: основна + напій = -25 грн
        if (order.CreatedAt.Hour >= 12 && order.CreatedAt.Hour < 16)
            disc += Math.Min(mains, drinks) * 25;
        if (order.Coupon != null)
        {
            switch (order.Coupon.Trim().ToUpper())
            {
                case "STUDENT":
                    disc += (sum - disc) * 0.1m;
                    break;
                case "STAFF":
                    // персонал: пів ціни, без сервісу
                    disc += (sum - disc) / 2;
                    waiveService = true;
                    break;
                default:
                    Console.WriteLine("  ? coupon " + order.Coupon + " not recognised");
                    break;
            }
        }
        // сервіс у залі: з 6 гостей 10%, з 10 - 12%; бар - без
        int pct = 0;
        decimal pack = 0;
        if (order.IsTakeaway)
            pack = cnt * 5;
        else if (order.TableNo < 40)
        {
            if (order.Guests > 6)
                pct = 10;
            if (order.Guests >= 10)
                pct = 12;
        }
        //if (order.Guests > 12)
        //    pct = 15;     // банкети - окремим прайсом
        if (waiveService)
            pct = 0;
        decimal total = sum - disc;
        total = total + Math.Round(total * pct / 100, 2) + pack;
        // готівка - до 10 коп.
        if (payment == "CASH")
            total = Math.Round(total, 1);
        else
            total = Math.Round(total, 2);
        LastDiscount = disc;
        LastServicePct = pct;
        AuditLog.Write("total #" + order.Id + " " + total + " " + payment);
        return total;
    }

    // chef's soup of the day, none at weekends
    public string SoupOfDay()
    {
        switch (DateTime.Today.DayOfWeek)
        {
            case DayOfWeek.Monday: return "BOR";
            case DayOfWeek.Tuesday: return "MUS";
            case DayOfWeek.Wednesday: return "CHK";
            case DayOfWeek.Thursday: return "PEA";
            case DayOfWeek.Friday: return "BOR";
            default: return "";
        }
    }

    public MenuItem Find(string code)
    {
        if (code == null)
            return null;
        return repo.findItem(code.Trim().ToUpper());
    }

    // старий happy hour 17-19, до 2023 р.
    private bool happyHour(DateTime t)
    {
        return t.Hour >= 17 && t.Hour < 19;
    }
}
