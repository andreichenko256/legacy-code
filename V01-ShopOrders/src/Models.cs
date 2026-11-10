namespace ShopOrders;

public enum OrderStatus { New, Paid, Shipped, Delivered, Cancelled }

public class Product
{
    public string Sku, Name;
    public decimal Price;               // net, without VAT
    public int Stock;
    public string Category;             // PC, ACC, SALE
}

public class Customer
{
    public int Id;
    public string Name, Email;
    public string Phone;                // +380XXXXXXXXX
    public string Level = "NONE";       // NONE/SILVER/GOLD/PLATINUM
    public DateTime RegisteredOn;
    public bool IsBlocked;
    public decimal TotalSpent;          // оновлюється при доставці
}

public class OrderLine
{
    public string Sku;
    public int Qty;
    public decimal UnitPrice, LineDiscount;     // заповнює OrderProcessor
}

// Усі суми заповнює OrderProcessor.Process
public class Order
{
    public int Id;
    public int CustomerId;
    public List<OrderLine> Lines = new List<OrderLine>();
    public OrderStatus Status = OrderStatus.New;
    public string AppliedPromo;
    public string PayMethod;            // CARD, PARTS, INVOICE
    public decimal Subtotal;
    public decimal Discount;
    public decimal Shipping;
    public decimal Vat;
    public decimal Fee;
    public decimal Total;
    public DateTime CreatedAt;

    public void AddLine(string sku, int qty)
    {
        OrderLine l = new OrderLine();
        l.Sku = sku;
        l.Qty = qty;
        Lines.Add(l);
    }
}

public class PromoCode
{
    public string Code;
    public int Percent, UsesLeft;
    public DateTime ValidUntil;
}
