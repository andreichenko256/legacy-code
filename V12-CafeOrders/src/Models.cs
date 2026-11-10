namespace CafeOrders;

public enum OrderStatus { Open, Served, Paid, Cancelled }

public class Order
{
    public int Id;
    public OrderStatus Status = OrderStatus.Open;   // Open -> Served -> Paid, Cancelled
    public int TableNo;             // 0 - навинос, 40+ - барна стійка
    public int Guests;
    public bool IsTakeaway;
    public string Waiter;
    public string Coupon;
    public DateTime CreatedAt;
    public List<OrderItem> Items = new List<OrderItem>();
    public string Payment;          // "CASH" / "CARD"
    public decimal PaidAmount;
    public decimal Tips;
    public DateTime ClosedAt;
}

// позиція меню
public class MenuItem
{
    public string Code;
    public string Name;
    public decimal Price;
    public string Category;         // DRINK, BAKERY, MAIN, SOUP, DESSERT
    public bool Available = true;   // false - стоп-лист
}

// рядок замовлення (ціна - на момент замовлення)
public class OrderItem
{
    public string Code;
    public string Name;
    public decimal Price;
    public string Category;
    public int Qty;

    public OrderItem(string code, string name, decimal price, string category, int qty)
    {
        Code = code;
        Name = name;
        Price = price;
        Category = category;
        Qty = qty;
    }
}
