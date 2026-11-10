namespace PharmacyStock;

// Усе в пам'яті; 1С забирає вивантаження раз на добу
public class StockRepository
{
    private static int nextNo = 1;   // спільна нумерація для всіх кас
    public List<Medicine> Medicines = new List<Medicine>();
    public List<StockBatch> Batches = new List<StockBatch>();
    public List<Prescription> Prescriptions = new List<Prescription>();

    public string NextNumber()
    {
        return "RX-" + (nextNo++).ToString("D6");
    }

    public StockBatch AddBatch(string code, string batchNo, DateTime expiry, int qty)
    {
        StockBatch b = new StockBatch();
        b.MedicineCode = code;
        b.BatchNo = batchNo;
        b.ExpiryDate = expiry;
        b.Qty = qty;
        Batches.Add(b);
        return b;
    }

    public Medicine FindMedicine(string code)
    {
        foreach (Medicine m in Medicines)
            if (m.Code == code)
                return m;
        return null;
    }

    public Prescription FindPrescription(string no)
    {
        foreach (Prescription p in Prescriptions)
            if (p.Number == no)
                return p;
        return null;
    }

    // тільки придатні партії, найближчий термін першим
    public List<StockBatch> BatchesFor(string code)
    {
        List<StockBatch> res = new List<StockBatch>();
        foreach (StockBatch b in Batches)
        {
            if (b.MedicineCode != code)
                continue;
            int i = 0;
            while (i < res.Count && res[i].ExpiryDate <= b.ExpiryDate)
                i++;
            res.Insert(i, b);
        }
        return res;
    }
}
