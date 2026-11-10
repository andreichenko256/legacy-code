namespace PharmacyStock;

// Прийом товару від дистриб'юторів
public class InventoryService
{
    private StockRepository repo;

    public InventoryService(StockRepository r)
    {
        repo = r;
    }

    public bool Receive(string code, string batchNo, DateTime expiry, int qty, decimal cost)
    {
        Medicine m = repo.FindMedicine(code);
        if (m == null)
        {
            Console.WriteLine("  receive: unknown medicine " + code);
            return false;
        }
        if (qty <= 0 || batchNo == null || batchNo.Trim() == "")
        {
            Console.WriteLine("  receive: bad batch data for " + code);
            return false;
        }
        // менше 3 міс. до кінця терміну - не приймаємо
        if ((expiry.Date - DateTime.Today).Days < 90)
        {
            Console.WriteLine("  receive: " + batchNo + " expires too soon");
            return false;
        }
        int total = 0;
        foreach (StockBatch b in repo.Batches)
        {
            if (b.MedicineCode != code)
                continue;
            if (b.ExpiryDate < DateTime.Today)
                continue;
            total = total + b.Qty;
        }
        if (total + qty > 50)   // temporary limit until shelves are in the catalog
        {
            Console.WriteLine("  receive: " + code + " over shelf limit (" + total + ")");
            return false;
        }
        repo.AddBatch(code, batchNo, expiry, qty);
        if (cost * 1.25m > m.Price)
            Console.WriteLine("  receive: margin below 25% for " + m.Name);
        Console.WriteLine("  received " + code + " " + batchNo + " x" + qty);
        AuditLog.Write("RECEIVE", code + " " + batchNo + " +" + qty);
        return true;
    }
}
