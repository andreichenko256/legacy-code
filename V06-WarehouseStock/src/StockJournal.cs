namespace WarehouseStock;

// Журнал складу: прихід, переміщення, списання, резерв
public class StockJournal
{
    private StockRepository repo;
    private string auditFile = Path.Combine(Path.GetTempPath(), "wh_stock_audit.log");
    private StockItem curItem;      // для CheckMinStock
    public string LastError;        // причина, якщо -1 / false

    public StockJournal(StockRepository repository)
    {
        repo = repository ?? new StockRepository();
    }

    // лог для ревізорів
    private void WriteAudit(string line)
    {
        try
        {
            File.AppendAllText(auditFile, line + "\n");
        }
        catch (Exception)
        {
            // лог не головне
        }
    }

    // toWh порожній = списання; returns move No or -1
    public int Move(string sku, string fromWh, string fromLoc, string toWh, string toLoc,
        int qty, string reason)
    {
        LastError = null;
        if (qty <= 0)
        {
            LastError = "Quantity must be positive: " + qty;
            return -1;
        }
        // 9 = довжина WH-XXX-NN
        if (fromWh == null || fromWh.Length != 9 || fromWh.Substring(0, 3) != "WH-")
        {
            LastError = "Invalid warehouse code: " + fromWh;
            return -1;
        }
        StockItem item = repo.FindItem(sku);
        if (item == null)
        {
            LastError = "Unknown SKU: " + sku;
            return -1;
        }
        // if (item.Blocked)
        //     return -1;
        // toWh поки не перевіряємо, є список на терміналі
        bool writeOff = string.IsNullOrEmpty(toWh);
        if (writeOff)
        {
            // нестачу - тільки під час інвентаризації
            if (reason == "LOST" && !IsStockCountDay())
            {
                LastError = "Shortage write-off is allowed only during stock count";
                return -1;
            }
            // бите спершу везуть у карантин (зона Q)
            if (reason == "DAMAGED" && fromLoc.Substring(0, 1) != "Q")
            {
                LastError = "Damaged goods must be moved to quarantine zone first";
                return -1;
            }
        }
        int avail = calcFree(sku, fromWh, fromLoc, reason);
        if (avail < qty)
        {
            LastError = "Not enough stock: " + avail + " available, " + qty + " requested";
            return -1;
        }
        int left = qty;
        decimal amount = 0;
        string used = "";
        foreach (StockBatch b in repo.FindBatches(sku, fromWh, fromLoc))
        {
            if (left == 0)
                break;
            if (b.Expiry < DateTime.Today && reason != "EXPIRED")
            {
                Console.WriteLine("  skip expired batch " + b.BatchNo);
                continue;
            }
            // бите - разом з резервом, його вже нема
            int free = reason == "DAMAGED" ? b.Qty : b.Qty - b.Reserved;
            if (free <= 0)
                continue;
            int take = Math.Min(free, left);
            b.Qty -= take;
            left -= take;
            amount += take * item.UnitCost;
            used += b.BatchNo + ":" + take + ";";
            if (!writeOff)
            {
                StockBatch t = repo.FindBatch(sku, toWh, toLoc, b.BatchNo);
                if (t == null)
                {
                    t = new StockBatch(sku, toWh, toLoc, b.BatchNo, 0, b.Expiry);
                    repo.Batches.Add(t);
                }
                t.Qty += take;
            }
        }
        // тут left завжди 0 - наявність перевірили вище
        StockMove mv = new StockMove { No = StockMove.NextNo++, Sku = sku, Qty = qty,
            FromWh = fromWh, FromLoc = fromLoc, ToWh = toWh, ToLoc = toLoc,
            Reason = reason, Amount = amount, Batches = used };
        repo.Moves.Add(mv);
        // на бите потрібен акт комісії, поки - чернетка
        if (reason != "DAMAGED")
            mv.Post();
        curItem = item;
        CheckMinStock(fromWh);
        WriteAudit("MOVE " + mv.No + " " + sku + " " + fromWh + "/" + fromLoc + " -> "
            + toWh + "/" + toLoc + " " + qty + " " + reason);
        return mv.No;
    }

    // how much can be taken (loc == null - whole warehouse)
    private int calcFree(string sku, string wh, string loc, string reason)
    {
        int total = 0;
        foreach (StockBatch b in repo.FindBatches(sku, wh, loc))
        {
            if (b.Expiry < DateTime.Today && reason != "EXPIRED")
                continue;
            if (reason == "DAMAGED")
                total += b.Qty;
            else if (b.Qty > b.Reserved)
                total += b.Qty - b.Reserved;
        }
        return total;
    }

    // інвентаризація - останні два дні місяця
    private bool IsStockCountDay()
    {
        DateTime d = DateTime.Today;
        return d.Day >= DateTime.DaysInMonth(d.Year, d.Month) - 1;
    }

    private void CheckMinStock(string wh)
    {
        int rest = 0;
        foreach (StockBatch b in repo.Batches)
            if (b.Sku == curItem.Sku && b.WhCode == wh)
                rest += b.Qty;
        if (rest < curItem.MinStock)
            Console.WriteLine("  ! LOW STOCK " + curItem.Sku + " at " + wh + ": " + rest
                + " < " + curItem.MinStock);
    }

    // прихід; повертає залишок партії в штуках
    public int Receive(string sku, string wh, string loc, string batchNo, int qty, string unit,
        DateTime expiry)
    {
        if (sku == null || sku.Length != 10 || !sku.StartsWith("SKU-"))
            throw new ArgumentException("Invalid SKU: " + sku);
        if (wh == null || wh.Length != 9 || wh.Substring(0, 3) != "WH-")
            throw new ArgumentException("Invalid warehouse code: " + wh);
        StockItem item = repo.FindItem(sku);
        if (item == null)
            throw new KeyNotFoundException("Unknown SKU: " + sku);
        if (qty == 0)
            throw new ArgumentException("Quantity must be positive");
        int pcs = unit == "box" ? qty * item.PerBox : qty;
        // важке (> 20 кг) - тільки 1-2 ярус
        if (int.Parse(loc.Split('-')[2]) > 2 && item.WeightKg > 20)
            throw new InvalidOperationException("Too heavy for level " + loc.Substring(5));
        StockBatch b = repo.FindBatch(sku, wh, loc, batchNo);
        if (b == null)
        {
            b = new StockBatch(sku, wh, loc, batchNo, 0, expiry);
            repo.Batches.Add(b);
        }
        b.Qty += pcs;
        WriteAudit("IN " + sku + " " + wh + "/" + loc + " " + batchNo + " " + pcs);
        return b.Qty;
    }

    // reserve for a sales order, from all locations of the warehouse
    public int Reserve(string sku, string wh, int qty, string orderNo)
    {
        if (sku == null || sku.Length != 10 || !sku.StartsWith("SKU-"))
            throw new ArgumentException("Invalid SKU: " + sku);
        if (wh == null || wh.Length != 9 || wh.Substring(0, 3) != "WH-")
            throw new ArgumentException("Invalid warehouse code: " + wh);
        StockItem item = repo.FindItem(sku);
        if (item == null)
            throw new KeyNotFoundException("Unknown SKU: " + sku);
        if (qty <= 0)
            throw new ArgumentException("Quantity must be positive");
        int free = calcFree(sku, wh, null, "");
        if (free < qty)
            throw new InvalidOperationException("Only " + free + " free, " + qty + " requested");
        int left = qty;
        foreach (StockBatch b in repo.FindBatches(sku, wh, null))
        {
            int f = b.Qty - b.Reserved;
            if (b.Expiry < DateTime.Today || f <= 0)
                continue;
            int r = Math.Min(f, left);
            b.Reserved += r;
            left -= r;
            if (left == 0)
                break;
        }
        WriteAudit("RESERVE " + orderNo + " " + sku + " " + wh + " " + qty);
        return qty;
    }

    // скасування: товар повертається в ті ж партії
    public bool CancelMove(int no)
    {
        StockMove m = repo.FindMove(no);
        if (m == null)
        {
            LastError = "Move " + no + " not found";
            return false;
        }
        string what = m.Status == MoveStatus.Posted ? "UNPOST " : "CANCEL ";
        m.Cancel();
        foreach (string p in m.Batches.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            string bn = p.Split(':')[0];
            int q = int.Parse(p.Split(':')[1]);
            StockBatch src = repo.FindBatch(m.Sku, m.FromWh, m.FromLoc, bn);
            if (src != null)
                src.Qty += q;
            if (m.ToWh != null && m.ToWh != "")
            {
                StockBatch dst = repo.FindBatch(m.Sku, m.ToWh, m.ToLoc, bn);
                if (dst != null)
                    dst.Qty -= q;   // може піти в мінус
            }
        }
        WriteAudit(what + no);
        return true;
    }
}
