namespace ShopOrders;

public class OrderService
{
    private ShopRepository repo;
    private static bool SendSms = false;    // SMS gateway contract ended in 2023

    public OrderService(ShopRepository r)
    {
        repo = r;
    }

    // New -> Paid/Cancelled, Paid -> Shipped/Cancelled, Shipped -> Delivered.
    // Returns false if the move is not allowed or the order is not found.
    public bool ChangeStatus(int orderId, OrderStatus to)
    {
        Order o = repo.GetOrder(orderId);
        if (o == null)
        {
            Console.WriteLine("  order #" + orderId + " not found");
            return false;
        }
        bool allowed = false;
        switch (o.Status)
        {
            case OrderStatus.New:
                allowed = to == OrderStatus.Paid || to == OrderStatus.Cancelled;
                break;
            case OrderStatus.Paid:
                allowed = to == OrderStatus.Shipped || to == OrderStatus.Cancelled;
                break;
            case OrderStatus.Shipped:
                allowed = to == OrderStatus.Delivered;
                break;
        }
        if (!allowed)
        {
            Console.WriteLine("  cannot move #" + o.Id + " from " + o.Status + " to " + to);
            return false;
        }
        if (to == OrderStatus.Cancelled)
        {
            CalcRefund(o);
            restock_items(o);
        }
        if (to == OrderStatus.Delivered)
        {
            Customer c = repo.FindCustomer(o.CustomerId);
            c.TotalSpent += o.Total;
            new CustomerService().UpdateLevel(c);
        }
        if (SendSms)
            Console.WriteLine("SMS " + repo.FindCustomer(o.CustomerId).Phone + ": " + to);
        o.Status = to;
        return true;
    }

    // скільки повернути при скасуванні
    public decimal CalcRefund(Order o)
    {
        decimal refund = 0m;
        if (o.Status == OrderStatus.Paid || o.Status == OrderStatus.Shipped)
        {
            refund = o.Total;
            if (o.PayMethod == "PARTS")
                refund = refund - o.Fee;    // bank keeps its fee
        }
        Console.WriteLine("  refund for #" + o.Id + ": " + refund.ToString("0.00"));
        return refund;
    }

    // for the cart page: delivery estimate before checkout
    public decimal EstimateShipping(decimal subtotal, decimal discount, bool express)
    {
        decimal goods = subtotal - discount;
        decimal ship = 0m;
        if (goods < 1500m)
            ship = 79m;
        if (express)
            ship += 150m;
        return ship;
    }

    private void restock_items(Order o)
    {
        foreach (OrderLine l in o.Lines)
        {
            try { repo.FindProduct(l.Sku).Stock += l.Qty; }
            catch (NullReferenceException) { }  // товар могли видалити
        }
    }
}
