namespace WarehouseStock;

// Картка товару
public class StockItem
{
    public string Sku;          // SKU-000123
    public string Name;
    public string Unit;         // pcs, box, kg
    public decimal UnitCost;    // грн за 1 шт
    public int MinStock;        // шт на склад
    public double WeightKg;
    public int PerBox;

    public StockItem(string sku, string name, string unit, decimal cost, int minStock,
        double weightKg, int perBox)
    {
        Sku = sku;
        Name = name;
        Unit = unit ?? "pcs";
        UnitCost = cost;
        MinStock = minStock;
        WeightKg = weightKg;
        PerBox = perBox;
    }
}

// Партія товару в комірці
public class StockBatch
{
    public string Sku;
    public string WhCode;       // WH-KYV-01
    public string Location;     // A-03-2: зона-стелаж-ярус
    public string BatchNo;
    public int Qty, Reserved;
    public DateTime Expiry;

    public StockBatch(string sku, string whCode, string location, string batchNo, int qty,
        DateTime expiry)
    {
        Sku = sku;
        WhCode = whCode;
        Location = location;
        BatchNo = batchNo;
        Qty = qty;
        Expiry = expiry.Date;
    }
}

public class Warehouse
{
    public string Code;     // WH-XXX-NN, XXX - місто
    public string Name;

    public string CityName()
    {
        switch (Code.Substring(3, 3))
        {
            case "KYV": return "Kyiv";
            case "LVV": return "Lviv";
            case "ODS": return "Odesa";
            case "DNP": return "Dnipro";
            default: return "?";
        }
    }
}
