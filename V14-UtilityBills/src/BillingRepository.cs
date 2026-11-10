namespace UtilityBills;

public class BillingRepository
{
    public static int nextBillNo = 5001;

    public List<Account> Accounts = new List<Account>();
    public List<Bill> Bills = new List<Bill>();

    public BillingRepository()
    {
        AddAccount("UB-1001", "Olena Kovalenko", "Sadova 4/12", 2, "STD", "1Z", false, 0);
        AddAccount("UB-1002", "Mykola Shevchenko", "Sadova 4/31", 1, "VET", "1Z", false, 0);
        AddAccount("UB-1003", "Tetiana Bondarenko", "Lisova 9", 6, "LRG", "1Z", false, 600);
        AddAccount("UB-1004", "Ivan Petrenko", "Lisova 15", 3, "STD", "1Z", true, 0);
        AddAccount("UB-1005", "Hanna Moroz", "Sadova 6/7", 2, "CHN", "2Z", false, 250);
        // рахунки за минулий місяць зі старої бази
        SeedBill("UB-1002", "EL", 120, 262.8, BillStatus.Issued, -4);
        SeedBill("UB-1003", "WATER", 16, 600, BillStatus.Issued, 0);
        SeedBill("UB-1001", "GAS", 52, 451.42, BillStatus.Paid, -10);
    }

    private void AddAccount(string no, string owner, string addr, int residents, string cat,
        string meter, bool eheat, double cap)
    {
        Account a = new Account();
        a.Number = no;
        a.Owner = owner;
        a.Address = addr;
        a.Residents = residents;
        a.Category = cat;
        a.MeterType = meter;
        a.ElectricHeating = eheat;
        a.SubsidyCap = cap;
        Accounts.Add(a);
    }

    private void SeedBill(string acc, string svc, double units, double amount, BillStatus st,
        int dueIn)
    {
        Bill b = new Bill();
        b.Number = nextBillNo++;
        b.AccountNo = acc;
        b.Service = svc;
        b.Units = units;
        b.Amount = amount;
        b.Status = st;
        b.DueDate = DateTime.Today.AddDays(dueIn);
        b.IssuedOn = b.DueDate.AddDays(-20);
        if (st == BillStatus.Paid)
            b.PaidSum = amount;
        Bills.Add(b);
    }

    public Account FindAccount(string no)
    {
        foreach (Account a in Accounts)
        {
            if (a.Number == no)
                return a;
        }
        return null;
    }

    public Bill FindBill(int no)
    {
        foreach (Bill b in Bills)
        {
            if (b.Number == no)
                return b;
        }
        return null;
    }
}

public class AuditLog
{
    public static string FilePath =
        Path.Combine(Path.GetTempPath(), "utility_bills_audit.log");

    public static void Write(string msg)
    {
        try
        {
            string stamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            File.AppendAllText(FilePath, stamp + " " + msg + "\n");
        }
        catch
        {
        }
    }
}
