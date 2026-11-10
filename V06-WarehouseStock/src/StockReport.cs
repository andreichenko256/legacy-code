namespace WarehouseStock;

// Звіти для комірників
public class StockReport
{
    private StockRepository repo;

    public StockReport(StockRepository repo)
    {
        this.repo = repo;
    }

    // stock card for one warehouse; returns total pcs
    public int PrintCard(string sku, string wh, bool showEmpty)
    {
        if (sku == null || sku.Length != 10 || !sku.StartsWith("SKU-"))
            throw new ArgumentException("Invalid SKU: " + sku);
        if (wh == null || wh.Length != 9 || wh.Substring(0, 3) != "WH-")
            throw new ArgumentException("Invalid warehouse code: " + wh);
        StockItem item = repo.FindItem(sku);
        if (item == null)
            throw new KeyNotFoundException("Unknown SKU: " + sku);
        Console.WriteLine("Card " + sku + " " + item.Name + " @ " + wh + " (" + CityOf(wh) + ")");
        int total = 0;
        decimal sum = 0;
        foreach (StockBatch b in repo.FindBatches(sku, wh, null))
        {
            if (b.Qty == 0 && !showEmpty)
                continue;
            string flg = b.Expiry < DateTime.Today ? " EXPIRED" : "";
            Console.WriteLine("  " + b.Location + " " + b.BatchNo + " qty " + b.Qty
                + " res " + b.Reserved + flg);
            total += b.Qty;
            sum += b.Qty * item.UnitCost;
        }
        string boxes = item.PerBox > 1 ? " = " + total / item.PerBox + " box" : "";
        Console.WriteLine("  total " + total + " pcs" + boxes + ", value " + sum.ToString("0.00"));
        return total;
    }

    public string CityOf(string wh)
    {
        switch (wh.Substring(3, 3))
        {
            case "KYV": return "Kyiv";
            case "LVV": return "Lviv";
            case "ODS": return "Odesa";
            case "DNP": return "Dnipro";
            case "KHK": return "Kharkiv";
        }
        return "?";
    }

    public void LowStock(string wh)
    {
        foreach (StockItem it in repo.Items)
        {
            int rest = 0;
            foreach (StockBatch b in repo.Batches)
                if (b.Sku == it.Sku && b.WhCode == wh)
                    rest += b.Qty;
            if (rest < it.MinStock)
                Console.WriteLine("  ! LOW STOCK " + it.Sku + " at " + wh + ": " + rest
                    + " < " + it.MinStock);
        }
    }

    public string FormatMove(StockMove m)
    {
        string to = string.IsNullOrEmpty(m.ToWh) ? "WRITE-OFF" : m.ToWh + "/" + m.ToLoc;
        return "#" + m.No + " " + m.Sku + " " + m.FromWh + "/" + m.FromLoc + " -> " + to
            + " x" + m.Qty + " " + m.Reason + " " + m.Amount.ToString("0.00")
            + " [" + m.Status.ToString().ToUpper() + "]";
    }
}
