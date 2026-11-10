namespace ShopOrders;

public class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine("=== ShopOrders demo ===");
        ShopRepository repo = new ShopRepository();
        SeedCatalog(repo);
        OrderProcessor proc = new OrderProcessor(repo);
        Console.WriteLine("--- checkout ---");
        Order o1 = NewOrder(1, "MN-270", 1, "CB-USB", 2);
        Show(proc, proc.Process(o1, null, "card", false));
        Order o2 = NewOrder(2, "MS-202", 12, "CB-USB", 10);
        Show(proc, proc.Process(o2, "autumn10", "PARTS", false));
        Order o3 = NewOrder(3, "HS-OLD", 12, "ms-202", 1);
        Show(proc, proc.Process(o3, "SPRING15", "INVOICE", false));
        Order o4 = NewOrder(4, "CB-USB", 3, "XX-999", 1);
        Show(proc, proc.Process(o4, "", "crypto", false));
        Console.WriteLine("--- rejected ---");
        Show(proc, proc.Process(new Order(), null, "CARD", false));
        Show(proc, proc.Process(NewOrder(99, "MS-202", 1, "CB-USB", 1), null, "CARD", false));
        Show(proc, proc.Process(NewOrder(5, "MS-202", 1, "CB-USB", 1), null, "CARD", false));
        Console.WriteLine("Orders processed: " + OrderProcessor.ProcessedCount);
        RunStatuses(new OrderService(repo), o1.Id, o3.Id);
        new InvoicePrinter().PrintInvoice(o2, repo.FindCustomer(2));
        new InvoicePrinter().PrintInvoice(o4, repo.FindCustomer(4));
        Console.WriteLine(new CustomerService().DescribeBenefits(repo.FindCustomer(1)));
    }

    static void SeedCatalog(ShopRepository repo)
    {
        repo.AddProduct("MS-202", "Wireless mouse", 640m, 120, "PC");
        repo.AddProduct("CB-USB", "USB-C cable 1 m", 95m, 500, "ACC");
        repo.AddProduct("MN-270", "Monitor 27 inch", 7400m, 15, "PC");
        repo.AddProduct("HS-OLD", "Headset, last season", 520m, 30, "SALE");
        try { repo.AddProduct("GC-000", "Gift card", 0m, 10, "GIFT"); }
        catch (ArgumentException ex) { Console.WriteLine("Catalog error: " + ex.Message); }
        DateTime t = DateTime.Today;
        repo.AddCustomer(1, "Olena Koval", "olena@ukr.net", "+380671234567", "NONE",
            t.AddDays(-10));
        repo.AddCustomer(2, "Andrii Melnyk", "andrii@ukr.net", "+380501112233", "SILVER",
            t.AddDays(-400));
        repo.AddCustomer(3, "Iryna Bondar", "iryna@ukr.net", "+380931234567", "GOLD",
            t.AddDays(-900));
        repo.AddCustomer(4, "Kvant LLC", "office@kvant.ua", "+380442223344", "PLATINUM",
            t.AddDays(-1500));
        repo.AddCustomer(5, "Petro Sydorenko", "petro@ukr.net", "+380661231231", "NONE",
            t.AddDays(-60)).IsBlocked = true;
        Customer noMail = repo.AddCustomer(6, "Taras", "taras.ukr.net", "", "NONE", t);
        Console.WriteLine("Customer without e-mail accepted: " + (noMail != null));
        repo.AddPromo("AUTUMN10", 10, t.AddDays(30), 100);
        repo.AddPromo("SPRING15", 15, t.AddDays(-5), 100);
        Console.WriteLine("Catalog: " + repo.Products.Count + " products, "
            + repo.Customers.Count + " customers, " + repo.Promos.Count + " promo codes");
    }

    static void RunStatuses(OrderService svc, int first, int third)
    {
        Console.WriteLine("--- order status ---");
        Move(svc, first, OrderStatus.Delivered);
        Move(svc, first, OrderStatus.Shipped);
        Move(svc, first, OrderStatus.Delivered);
        Move(svc, first, OrderStatus.Cancelled);
        Move(svc, third, OrderStatus.Paid);
        Move(svc, third, OrderStatus.Cancelled);
        Move(svc, third, OrderStatus.Paid);
        Move(svc, 9999, OrderStatus.Shipped);
        Console.WriteLine("Delivery estimate for 1200: " + svc.EstimateShipping(1200m, 0m, false));
    }

    static void Move(OrderService svc, int id, OrderStatus to)
    {
        Console.WriteLine("  #" + id + " -> " + to + ": " + svc.ChangeStatus(id, to));
    }

    static Order NewOrder(int customerId, string sku1, int qty1, string sku2, int qty2)
    {
        Order o = new Order();
        o.CustomerId = customerId;
        o.AddLine(sku1, qty1);
        o.AddLine(sku2, qty2);
        return o;
    }

    static void Show(OrderProcessor proc, decimal result)
    {
        if (result < 0)
            Console.WriteLine("  REJECTED: " + proc.LastError);
        else
            Console.WriteLine("  to pay: " + result.ToString("0.00"));
    }
}
