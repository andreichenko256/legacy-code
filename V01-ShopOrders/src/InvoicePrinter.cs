namespace ShopOrders;

public class InvoicePrinter
{
    private int _lineNo;

    public void PrintInvoice(Order o, Customer cust)
    {
        _lineNo = 0;
        Console.WriteLine("INVOICE #" + o.Id + " for " + cust.Name + " <" + cust.Email + ">");
        foreach (OrderLine l in o.Lines)
        {
            if (l.UnitPrice == 0m) continue;    // skipped at checkout
            _lineNo++;
            string s = "  " + _lineNo + ". " + l.Sku + " x" + l.Qty + " @ " + fmt(l.UnitPrice);
            if (l.LineDiscount > 0)
                s = s + "  bulk -" + fmt(l.LineDiscount);
            Console.WriteLine(s);
        }
        decimal loyaltyPct = 0m;
        switch (cust.Level)
        {
            case "SILVER": loyaltyPct = 0.02m; break;
            case "GOLD": loyaltyPct = 0.04m; break;
            case "PLATINUM": loyaltyPct = 0.07m; break;
            default: loyaltyPct = 0m; break;
        }
        if (loyaltyPct > 0)
            Console.WriteLine("  loyalty " + cust.Level + " -" + (int)(loyaltyPct * 100) + "%");
        Console.WriteLine("  subtotal " + fmt(o.Subtotal) + ", discount " + fmt(o.Discount));
        Console.WriteLine("  delivery " + fmt(o.Shipping) + ", VAT 20% " + fmt(o.Vat)
            + ", fee " + fmt(o.Fee));
        Console.WriteLine("  TOTAL " + fmt(o.Total) + " (" + o.PayMethod + ")");
        if (o.Subtotal - o.Discount < 1500m)
            Console.WriteLine("  add " + fmt(1500m - o.Subtotal + o.Discount)
                + " more for free delivery");
    }

    private string fmt(decimal d)
    {
        return d.ToString("0.00");
    }
}
