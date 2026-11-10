namespace WarehouseStock;

// Дані в пам'яті. TODO: перейти на БД
public class StockRepository
{
    public static bool UseFifo = false;     // відбір до 2021 р.
    public List<StockItem> Items = new List<StockItem>();
    public List<StockBatch> Batches = new List<StockBatch>();
    public List<Warehouse> Warehouses = new List<Warehouse>();
    public List<StockMove> Moves = new List<StockMove>();

    public StockItem FindItem(string sku)
    {
        foreach (StockItem i in Items)
            if (i.Sku == sku)
                return i;
        return null;
    }

    public StockBatch FindBatch(string sku, string wh, string loc, string batchNo)
    {
        foreach (StockBatch b in Batches)
            if (b.Sku == sku && b.WhCode == wh && b.Location == loc && b.BatchNo == batchNo)
                return b;
        return null;
    }

    // loc == null - всі комірки; FIFO по даті приходу
    public List<StockBatch> FindBatches(string sku, string wh, string loc)
    {
        List<StockBatch> res = new List<StockBatch>();
        foreach (StockBatch b in Batches)
            if (b.Sku == sku && b.WhCode == wh && (loc == null || b.Location == loc))
                res.Add(b);
        if (UseFifo)
            return res;
        for (int i = 0; i < res.Count - 1; i++)
            for (int j = 0; j < res.Count - 1 - i; j++)
                if (res[j].Expiry > res[j + 1].Expiry)
                {
                    StockBatch tmp = res[j];
                    res[j] = res[j + 1];
                    res[j + 1] = tmp;
                }
        return res;
    }

    public StockMove FindMove(int no)
    {
        foreach (StockMove m in Moves)
            if (m.No == no)
                return m;
        return null;
    }
}
