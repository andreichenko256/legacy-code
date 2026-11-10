namespace CafeOrders;

public class ShiftReport
{
    // guest bill
    public void PrintBill(Order order, MenuService menu, string payment)
    {
        decimal gross = 0;
        int cnt = 0;
        foreach (OrderItem it in order.Items)
        {
            if (it.Qty <= 0)
                continue;
            gross += it.Price * it.Qty;
            cnt += it.Qty;
        }
        decimal total = menu.Total(order, payment, false);
        Console.WriteLine("  bill #" + order.Id + (order.IsTakeaway ? ", takeaway" : ", table "
            + order.TableNo) + ", " + cnt + " item(s)");
        foreach (OrderItem it in order.Items)
            Console.WriteLine("    " + it.Qty + " x " + it.Name + "  " + it.Price * it.Qty);
        Console.WriteLine("    subtotal " + gross.ToString("0.00"));
        if (menu.LastDiscount > 0)
            Console.WriteLine("    discounts -" + menu.LastDiscount.ToString("0.00"));
        if (menu.LastServicePct > 0)
            Console.WriteLine("    service " + menu.LastServicePct + "%");
        Console.WriteLine("    to pay " + total.ToString("0.00") + " " + payment);
    }

    // звіт зміни: каса, чайові, сервіс
    public void PrintShift(List<Order> orders)
    {
        decimal cash = 0, card = 0, tips = 0, svc = 0, packs = 0;
        int paid = 0, cancelled = 0;
        foreach (Order order in orders)
        {
            if (order.Status != OrderStatus.Paid)
            {
                if (order.Status == OrderStatus.Cancelled)
                    cancelled++;
                continue;
            }
            paid++;
            decimal gross = 0;
            int cnt = 0;
            foreach (OrderItem it in order.Items)
            {
                if (it.Qty <= 0)
                    continue;
                gross += it.Price * it.Qty;
                cnt += it.Qty;
            }
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
            svc += gross * pct / 100;     // приблизно, без знижок
            packs += pack;
            if (order.Payment == "CASH")
                cash += order.PaidAmount;
            else
                card += order.PaidAmount;
            tips += order.Tips;
        }
        Console.WriteLine("  paid " + paid + ", cancelled " + cancelled
            + ", open " + (orders.Count - paid - cancelled));
        Console.WriteLine("  cash " + cash.ToString("0.00") + ", card " + card.ToString("0.00"));
        Console.WriteLine("  tips " + tips.ToString("0.00") + ", service ~" + svc.ToString("0.00")
            + ", packaging " + packs.ToString("0.00"));
    }
}
