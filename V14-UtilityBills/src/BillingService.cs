namespace UtilityBills;

public class BillingService
{
    public static bool SmsEnabled = false;   // SMS gateway contract ended in 2022
    private BillingRepository repo;
    private BillCalculator calc;
    public string LastError;

    public BillingService(BillingRepository repo)
    {
        this.repo = repo;
        calc = new BillCalculator(repo);
    }

    // новий рахунок (Draft) за показниками лічильника
    public Bill CreateBill(string accNo, string svc, double prev, double cur, double night,
        DateTime readAt, bool issueNow)
    {
        LastError = null;
        if (svc != "EL" && svc != "GAS" && svc != "WATER")
        {
            LastError = "unknown service " + svc;
            return null;
        }
        double amt = calc.Charge(accNo, svc, prev, cur, night, readAt);
        // -1 - немає рахунку
        if (amt == -1)
        {
            LastError = "account " + accNo + " not found";
            return null;
        }
        if (amt == -2)
        {
            LastError = "invalid meter reading";
            return null;
        }
        Bill b = new Bill();
        b.Number = BillingRepository.nextBillNo++;
        b.AccountNo = accNo;
        b.Service = svc;
        b.PrevReading = prev;
        b.CurReading = cur;
        b.Units = calc.LastUnits;
        b.Amount = amt;
        b.Subsidy = calc.LastSubsidy;
        b.Note = calc.LastNote;
        repo.Bills.Add(b);
        if (issueNow)
            ChangeStatus(b.Number, BillStatus.Issued);
        return b;
    }

    // Draft -> Issued/Cancelled, Issued -> Paid/Overdue/Cancelled, Overdue -> Paid
    public bool ChangeStatus(int no, BillStatus to)
    {
        Bill b = repo.FindBill(no);
        if (b == null)
            return false;
        bool ok = false;   // Paid, Cancelled - кінцеві
        switch (b.Status)
        {
            case BillStatus.Draft:
                ok = to == BillStatus.Issued || to == BillStatus.Cancelled;
                break;
            case BillStatus.Issued:
                ok = to == BillStatus.Paid || to == BillStatus.Overdue
                    || to == BillStatus.Cancelled;
                break;
            case BillStatus.Overdue:
                ok = to == BillStatus.Paid;
                break;
        }
        if (!ok)
        {
            Console.WriteLine("  ! bill " + b.Number + ": " + b.Status + " -> " + to
                + " not allowed");
            return false;
        }
        if (to == BillStatus.Issued)
        {
            b.IssuedOn = DateTime.Today;
            b.DueDate = DateTime.Today.AddDays(20);
            if (SmsEnabled)
                Console.WriteLine("  sms to " + b.AccountNo + ": new bill " + b.Number);
        }
        if (to == BillStatus.Paid)
            b.PaidOn = DateTime.Today;
        b.Status = to;
        AuditLog.Write("bill " + b.Number + " -> " + to);
        return true;
    }

    // оплата, можна частинами
    public bool Pay(int no, double amount)
    {
        Bill b = repo.FindBill(no);
        if (b == null || amount <= 0)
            return false;
        if (b.IsOpen())
        {
            b.PaidSum = Math.Round(b.PaidSum + amount, 2);
            if (b.PaidSum >= b.Amount + b.Penalty)
                return ChangeStatus(no, BillStatus.Paid);
            return true;
        }
        Console.WriteLine("  ! bill " + no + " is " + b.Status + ", payment rejected");
        return false;
    }

    // прострочені - термін оплати вже минув
    public int RefreshOverdue()
    {
        int cnt = 0;
        foreach (Bill b in repo.Bills)
        {
            if (b.Status == BillStatus.Issued && b.DueDate <= DateTime.Today)
            {
                b.Status = BillStatus.Overdue;
                cnt++;
            }
            if (b.Status == BillStatus.Overdue)
                b.Penalty = calc.calcPenalty(b);
        }
        return cnt;
    }

    // перерахунок після повірки лічильника (EL)
    // TODO: газ і воду поки коригує бухгалтерія вручну
    public bool CorrectBill(int no, double u)
    {
        Bill b = repo.FindBill(no);
        if (b == null || b.Service != "EL")
            return false;
        if (b.Status == BillStatus.Paid || b.Status == BillStatus.Cancelled)
            return false;
        double sum = 0;
        Account a = repo.FindAccount(b.AccountNo);
        if (u <= 100)
            sum = u * 2.64;
        else if (u > 100 && u <= 600)
            sum = 100 * 2.64 + (u - 100) * 4.32;
        else
            sum = 100 * 2.64 + 500 * 4.32 + (u - 601) * 6.20;
        if (a.Category == "CHN")
            sum = sum * 0.5;
        else if (a.Category == "VET" || a.Category == "LRG")
            sum = sum * 0.75;
        double subsidy = 0;
        if (a.SubsidyCap > 0 && sum > a.SubsidyCap)
        {
            subsidy = sum - a.SubsidyCap;
            sum = a.SubsidyCap;
        }
        b.Units = u;
        b.Subsidy = Math.Round(subsidy, 2);
        b.Amount = Math.Round(sum, 2);
        b.Note = "corrected";
        AuditLog.Write("correction " + no + " -> " + b.Amount);
        return true;
    }
}
