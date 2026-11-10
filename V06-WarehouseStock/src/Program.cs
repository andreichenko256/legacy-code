namespace WarehouseStock;

public class Program
{
    public static void Main(string[] args)
    {
        StockRepository repo = new StockRepository();
        Seed(repo);
        StockJournal journal = new StockJournal(repo);
        StockReport report = new StockReport(repo);
        Console.WriteLine("=== Warehouse stock: daily run ===");
        foreach (Warehouse w in repo.Warehouses)
            Console.WriteLine("  " + w.Code + " " + w.Name + " (" + w.CityName() + ")");
        Operations(journal);
        Console.WriteLine("--- documents ---");
        StockMove doc = repo.FindMove(1003);
        doc.SetNote("Act #15");
        Console.WriteLine("Post 1003 (draft) -> " + doc.Post());
        Console.WriteLine("Post 1003 again -> " + doc.Post());
        doc.SetNote("Act #16");
        Console.WriteLine("Edit posted 1003 -> note is still '" + doc.Note + "'");
        Console.WriteLine("Cancel 1004 -> " + journal.CancelMove(1004));
        try
        {
            journal.CancelMove(1004);
        }
        catch (InvalidOperationException e)
        {
            Console.WriteLine("Cancel 1004 again -> ERROR " + e.Message);
        }
        Console.WriteLine("Cancel 9999 -> " + journal.CancelMove(9999)
            + " (" + journal.LastError + ")");
        Console.WriteLine("--- reports ---");
        report.PrintCard("SKU-000123", "WH-KYV-01", false);
        report.PrintCard("SKU-000456", "WH-KYV-01", true);
        report.PrintCard("SKU-000789", "WH-KYV-01", true);
        report.LowStock("WH-KYV-01");
        foreach (StockMove m in repo.Moves)
            Console.WriteLine("  " + report.FormatMove(m));
        Console.WriteLine("=== done ===");
    }

    private static void Seed(StockRepository repo)
    {
        DateTime t = DateTime.Today;
        string kyv = "WH-KYV-01";
        List<StockBatch> bs = repo.Batches;
        repo.Warehouses.Add(new Warehouse { Code = kyv, Name = "Kyiv Central" });
        repo.Warehouses.Add(new Warehouse { Code = "WH-KYV-02", Name = "Kyiv Left Bank" });
        repo.Warehouses.Add(new Warehouse { Code = "WH-LVV-01", Name = "Lviv West" });
        repo.Items.Add(new StockItem("SKU-000123", "Coffee beans 1kg", "pcs", 420m, 20, 1, 10));
        repo.Items.Add(new StockItem("SKU-000456", "Water 1.5L", "box", 18.5m, 100, 1.6, 6));
        repo.Items.Add(new StockItem("SKU-000789", "Fridge 185cm", "pcs", 15999m, 8, 64, 1));
        bs.Add(new StockBatch("SKU-000123", kyv, "A-01-1", "B-2301", 40, t.AddDays(-3)));
        bs.Add(new StockBatch("SKU-000123", kyv, "A-01-1", "B-2302", 60, t.AddDays(90)));
        bs.Add(new StockBatch("SKU-000123", kyv, "A-01-2", "B-2303", 50, t.AddDays(200)));
        bs.Add(new StockBatch("SKU-000456", kyv, "B-02-1", "W-0501", 120, t.AddDays(30)));
        bs.Add(new StockBatch("SKU-000789", kyv, "C-01-1", "F-0007", 6, t.AddYears(10)));
        bs.Add(new StockBatch("SKU-000789", kyv, "Q-01-1", "F-0008", 2, t.AddYears(10)));
        bs[1].Reserved = 10;
        bs[5].Reserved = 1;
    }

    private static void Operations(StockJournal j)
    {
        Console.WriteLine("--- receiving ---");
        DateTime exp = DateTime.Today.AddDays(30);
        int n = j.Receive("SKU-000456", "WH-KYV-01", "B-02-1", "W-0501", 5, "box", exp);
        Console.WriteLine("Receive 5 box of water -> W-0501 = " + n + " pcs");
        n = j.Receive("SKU-000456", "WH-KYV-01", "B-02-1", "W-0501", 2, "Box", exp);
        Console.WriteLine("Receive 2 Box of water -> W-0501 = " + n + " pcs");
        try
        {
            j.Receive("SKU-000789", "WH-KYV-01", "C-03-3", "F-0009", 2, "pcs", exp);
        }
        catch (InvalidOperationException e)
        {
            Console.WriteLine("Receive 2 fridges to C-03-3 -> ERROR " + e.Message);
        }
        Console.WriteLine("--- moves ---");
        RunMove(j, "SKU-000123", "WH-KYV-01", "A-01-1", "WH-LVV-01", "B-01-1", 70, "TRANSFER");
        RunMove(j, "SKU-000123", "WH-KYV-01", "A-01-1", "WH-LVV-01", "B-01-1", 30, "TRANSFER");
        RunMove(j, "SKU-000123", "WH-KYV-01", "A-01-1", "", "", 40, "EXPIRED");
        RunMove(j, "SKU-000789", "WH-KYV-01", "C-01-1", null, null, 2, "DAMAGED");
        RunMove(j, "SKU-000789", "WH-KYV-01", "Q-01-1", null, null, 2, "DAMAGED");
        RunMove(j, "SKU-000456", "WH-KYV-01", "B-02-1", "WH-KYV-01", "B-03-1", 24, "TRANSFER");
        RunMove(j, "SKU-999999", "WH-KYV-01", "A-01-1", "WH-LVV-01", "B-01-1", 5, "TRANSFER");
        RunMove(j, "SKU-000123", "WH-KYV1", "A-01-1", "WH-LVV-01", "B-01-1", 5, "TRANSFER");
        RunMove(j, "SKU-000123", "WH-KYV-01", "A-01-2", "WH-LVV-01", "B-01-1", 0, "TRANSFER");
        n = j.Reserve("SKU-000456", "WH-KYV-01", 40, "SO-77");
        Console.WriteLine("Reserve 40 water -> " + n);
        try
        {
            j.Reserve("SKU-000123", "WH-KYV-01", 500, "SO-78");
        }
        catch (InvalidOperationException e)
        {
            Console.WriteLine("Reserve 500 coffee -> ERROR " + e.Message);
        }
    }

    private static void RunMove(StockJournal j, string sku, string fromWh, string fromLoc,
        string toWh, string toLoc, int qty, string reason)
    {
        int no = j.Move(sku, fromWh, fromLoc, toWh, toLoc, qty, reason);
        string to = string.IsNullOrEmpty(toWh) ? "write-off" : toWh + "/" + toLoc;
        string res = no > 0 ? "doc " + no : "ERROR " + j.LastError;
        Console.WriteLine("Move " + qty + " " + sku + " " + fromWh + "/" + fromLoc + " -> " + to
            + " " + reason + ": " + res);
    }
}
