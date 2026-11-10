namespace CafeOrders;

public class OrderDesk
{
    private CafeRepository repo;
    private MenuService menu;
    public string LastError = "";   // для екрана офіціанта

    public OrderDesk(CafeRepository repo)
    {
        this.repo = repo;
        menu = new MenuService(repo);
    }

    // Нове замовлення. Повертає номер або 0
    public int Open(int tableNo, int guests, bool takeaway, string waiter, string coupon)
    {
        if (!takeaway && (tableNo < 1 || tableNo > 45))
        {
            LastError = "no such table " + tableNo;
            return -1;
        }
        if (guests <= 0)
        {
            LastError = "guests must be positive";
            return -1;
        }
        // TODO: перевіряти, чи стіл вільний
        Order o = new Order();
        o.Id = CafeRepository.nextId++;
        o.TableNo = tableNo;
        o.Guests = guests;
        o.IsTakeaway = takeaway;
        o.Waiter = waiter;
        o.Coupon = coupon;
        o.CreatedAt = DateTime.Now;
        repo.Orders.Add(o);
        AuditLog.Write("open #" + o.Id + " table " + tableNo);
        return o.Id;
    }

    // Додати позицію; оплачене/скасоване не чіпаємо
    public bool AddItem(int orderId, string code, int qty)
    {
        Order o = repo.FindOrder(orderId);
        if (o == null)
        {
            LastError = "order " + orderId + " not found";
            return false;
        }
        if (o.Status == OrderStatus.Paid || o.Status == OrderStatus.Cancelled)
        {
            LastError = "order #" + o.Id + " is " + o.Status;
            return false;
        }
        MenuItem m = menu.Find(code);
        if (m == null || !m.Available)
        {
            LastError = code + (m == null ? " - unknown item" : " is in stop-list");
            return false;
        }
        if (qty <= 0 || qty > 20)
        {
            LastError = "invalid quantity " + qty;
            return false;
        }
        // дозамовлення після подачі - знову на кухню
        if (o.Status == OrderStatus.Served)
            o.Status = OrderStatus.Open;
        o.Items.Add(new OrderItem(m.Code, m.Name, m.Price, m.Category, qty));
        return true;
    }

    // рядок з планшета: "LAT*2, CRO, CHC*3"
    public int addFromSlip(int orderId, string text)
    {
        int n = 0;
        foreach (string part in text.Split(','))
        {
            string[] p = part.Trim().Split('*');
            int q = 1;
            if (p.Length > 1)
            {
                try { q = int.Parse(p[1]); }
                catch (Exception) { }       // планшет шле сміття - тоді 1
            }
            if (AddItem(orderId, p[0], q))
                n++;
        }
        return n;
    }

    // "same again" button - repeat the drinks round
    public int RepeatRound(int orderId)
    {
        Order o = repo.FindOrder(orderId);
        if (o == null)
            return 0;
        List<OrderItem> tmp = new List<OrderItem>();
        foreach (OrderItem it in o.Items)
        {
            if (it.Category == "DRINK")
                tmp.Add(new OrderItem(it.Code, it.Name, it.Price, it.Category, it.Qty));
        }
        o.Items.AddRange(tmp);
        return tmp.Count;
    }

    // Open -> Served -> Paid; оплата - тільки через Pay()
    public bool ChangeStatus(int id, OrderStatus to)
    {
        Order o = repo.FindOrder(id);
        if (o == null)
            return false;
        bool ok = false;
        switch (to)
        {
            case OrderStatus.Served:
                if (o.Status == OrderStatus.Open && o.Items.Count > 0)
                    ok = true;
                break;
            case OrderStatus.Cancelled:
                if (o.Status == OrderStatus.Open || o.Status == OrderStatus.Served)
                    ok = true;
                break;
        }
        if (!ok)
        {
            Console.WriteLine("  ! #" + id + ": " + o.Status + " -> " + to + " not allowed");
            return false;
        }
        o.Status = to;
        AuditLog.Write("order #" + id + " " + to);
        return true;
    }

    // Оплата. Повертає суму або -1
    public decimal Pay(int orderId, string payment, decimal tip, bool noService)
    {
        Order o = repo.FindOrder(orderId);
        if (o == null)
        {
            LastError = "order " + orderId + " not found";
            return -1;
        }
        if (o.Status == OrderStatus.Paid || o.Status == OrderStatus.Cancelled)
        {
            LastError = "order #" + o.Id + " is " + o.Status;
            return -1;
        }
        // зал платить після подачі, навинос - одразу
        if (o.Status == OrderStatus.Open && !o.IsTakeaway)
        {
            LastError = "order #" + o.Id + " is not served yet";
            return -1;
        }
        decimal sum = menu.Total(o, payment, noService);
        o.Payment = payment;
        o.PaidAmount = sum;
        o.Tips = tip;
        o.Status = OrderStatus.Paid;
        o.ClosedAt = DateTime.Now;
        AuditLog.Write("paid #" + o.Id + " " + sum + " " + payment);
        return sum;
    }

    // one-liner for the waiter screen
    public string Describe(Order order)
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
        string where = order.IsTakeaway ? "takeaway" : "table " + order.TableNo;
        return "#" + order.Id + " " + where + ", " + order.Guests + " guest(s), " + cnt
            + " item(s), " + gross.ToString("0.00") + ", " + order.Status;
    }
}
