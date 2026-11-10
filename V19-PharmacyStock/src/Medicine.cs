namespace PharmacyStock;

// Довідник препаратів. Code - артикул аптеки
public class Medicine
{
    public string Code;
    public string Name;
    public string Inn;              // діюча речовина (МНН)
    public decimal Price;           // роздріб, грн/упак.
    public bool IsControlled;       // наркотичні/психотропні
    public int MaxPerDispense = 99; // max packs per visit, 0 = no limit

    public Medicine(string code, string name, string inn, decimal price)
    {
        Code = code;
        Name = name;
        Inn = inn;
        Price = price;
    }
}

public class StockBatch
{
    public string MedicineCode;
    public string BatchNo;
    public DateTime ExpiryDate;
    public int Qty;

    public bool IsExpired()
    {
        return ExpiryDate < DateTime.Today;
    }
}
