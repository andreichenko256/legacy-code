namespace ShopOrders;

public class OrderProcessor
{
    public static int ProcessedCount = 0;
    private ShopRepository repo;
    private Customer _cust;             // для isFirstOrder
    public string LastError = "";

    public OrderProcessor()
    {
        repo = new ShopRepository();
    }

    public OrderProcessor(ShopRepository repository)
    {
        repo = repository;
    }

    // Checkout: goods, discounts, delivery, VAT, payment fee, status.
    // Returns the amount to pay or -1 if the order can't be placed (see LastError).
    public decimal Process(Order order, string promoCode, string payMethod, bool express)
    {
        LastError = "";
        if (order.Lines.Count == 0)
        {
            LastError = "Order has no lines";
            return -1;
        }
        _cust = repo.FindCustomer(order.CustomerId);
        if (_cust == null)
        {
            LastError = "Customer " + order.CustomerId + " not found";
            return -1;
        }
        if (_cust.IsBlocked)
        {
            LastError = "Customer " + _cust.Id + " is blocked";
            return -1;
        }
        string pm = payMethod.Trim().ToUpper();
        Console.WriteLine("Processing order for " + _cust.Name + " (" + _cust.Level + ")");
        // 1. товари + одразу резерв на складі
        decimal subtotal = 0m;
        decimal bulk = 0m;
        int items = 0;
        foreach (OrderLine line in order.Lines)
        {
            Product p = repo.FindProduct(line.Sku);
            // TEMP (03.2022): склад не синхронізовано
            if (p == null)
            {
                Console.WriteLine("  WARN: unknown SKU " + line.Sku + ", line skipped");
                continue;
            }
            line.UnitPrice = p.Price;
            decimal lineSum = p.Price * line.Qty;
            // оптова знижка 6% від 10 шт. (крім уцінки)
            if (line.Qty > 10)
            {
                if (p.Category != "SALE")
                {
                    line.LineDiscount = Math.Round(lineSum * 0.06m, 2);
                    bulk = bulk + line.LineDiscount;
                }
            }
            subtotal += lineSum;
            items += line.Qty;
            p.Stock = p.Stock - line.Qty;
        }
        subtotal = Math.Round(subtotal, 2);
        // 2. знижки: Silver 3%, Gold 5%, Platinum 7%
        decimal discount = bulk;
        decimal loyaltyPct = 0m;
        switch (_cust.Level)
        {
            case "SILVER": loyaltyPct = 0.02m; break;
            case "GOLD": loyaltyPct = 0.04m; break;
            case "PLATINUM": loyaltyPct = 0.07m; break;
            default: loyaltyPct = 0m; break;
        }
        decimal loyalty = Math.Round((subtotal - bulk) * loyaltyPct, 2);
        discount += loyalty;
        // новий клієнт (< 30 днів) і перше замовлення
        if ((DateTime.Now - _cust.RegisteredOn).TotalDays < 30 && isFirstOrder())
        {
            discount += Math.Round(subtotal * 0.1m, 2);
            Console.WriteLine("  welcome discount 10%");
        }
        PromoCode promo = repo.findPromo(promoCode);
        if (promo != null && promo.ValidUntil > DateTime.Now)
        {
            decimal promoSum = Math.Round(subtotal * promo.Percent / 100m, 2);
            discount += promoSum;
            promo.UsesLeft--;
            order.AppliedPromo = promo.Code;
            Console.WriteLine("  promo " + promo.Code + " -" + promo.Percent + "%");
        }
        // Black Friday 2021 - off until next year
        // if (DateTime.Now.Month == 11 && DateTime.Now.Day >= 24)
        //     discount += Math.Round(subtotal * 0.1m, 2);
        // 3. доставка 79 грн, від 1500 безкоштовно
        decimal goods = subtotal - discount;
        decimal ship = 0m;
        if (goods < 1500m)
            ship = 79m;
        if (express)
        {
            ship += 150m;
            // кур'єр у неділю - подвійний тариф
            if (DateTime.Now.DayOfWeek == DayOfWeek.Sunday)
                ship += 150m;
        }
        // 4. ПДВ 20%, доставка теж
        decimal vatBase = goods + ship;
        decimal vat = Math.Round(vatBase * 0.2m, 2);
        // 5. оплата
        decimal fee = 0m;
        if (pm == "PARTS")
            fee = Math.Round((goods + ship + vat) * 0.029m, 2);   // bank fee
        if (pm == "INVOICE")
            Console.WriteLine("  invoice issued, waiting for bank transfer");
        else
            order.Status = OrderStatus.Paid;
        // 6. save + log
        decimal total = goods + ship + vat + fee;
        order.Subtotal = subtotal;
        order.Discount = discount;
        order.Shipping = ship;
        order.Vat = vat;
        order.Fee = fee;
        order.Total = total;
        order.PayMethod = pm;
        order.CreatedAt = DateTime.Now;
        repo.SaveOrder(order);
        ProcessedCount++;
        Console.WriteLine("  order #" + order.Id + " [" + order.Status + "] goods "
            + subtotal.ToString("0.00") + ", discount " + discount.ToString("0.00")
            + ", delivery " + ship.ToString("0.00") + ", VAT " + vat.ToString("0.00"));
        string log = "ORDER " + order.Id + " cust=" + _cust.Id + " items=" + items;
        log += " sub=" + subtotal + " disc=" + discount + " ship=" + ship;
        log += " vat=" + vat + " fee=" + fee + " total=" + total + " pay=" + pm;
        WriteAudit(log);
        return total;
    }

    private bool isFirstOrder()
    {
        if (repo.Orders.Count == 0)
            return true;
        int cnt = 0;
        foreach (Order o in repo.Orders)
            if (o.CustomerId == _cust.Id && o.Status != OrderStatus.Cancelled)
                cnt++;
        return cnt == 0;
    }

    private decimal VatFromGross(decimal gross)     // prices had VAT before 2023
    {
        return Math.Round(gross * 20m / 120m, 2);
    }

    private void WriteAudit(string msg)
    {
        string stamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        string line = stamp + " | " + msg + "\n";
        try
        {
            string path = Path.Combine(Path.GetTempPath(), "shoporders_audit.log");
            File.AppendAllText(path, line);
        }
        catch (Exception)
        {
        }
    }
}
