namespace OnlineCourses;

// Оплата з внутрішнього балансу слухача.
// CORP: не списуємо, рахунок роботодавцю раз на місяць.
public class PaymentService
{
    public static int TxCount = 0;
    private AuditLog log = new AuditLog();

    public void Charge(Learner l, decimal amount)
    {
        if (amount < 0)
            throw new ArgumentException("Amount must not be negative");
        if (l.Plan == "CORP")
            l.CompanyDebt = l.CompanyDebt + amount;
        else if (l.Balance < amount)
            throw new InvalidOperationException("Insufficient balance");
        else
            l.Balance = l.Balance - amount;
        TxCount++;
        log.Write("CHARGE " + l.Id + " " + amount);
    }

    public void Refund(Learner l, decimal amount)
    {
        if (l.Plan == "CORP")
            l.CompanyDebt = l.CompanyDebt - amount;
        else
            l.Balance = l.Balance + amount;
        TxCount++;
        log.Write("REFUND " + l.Id + " " + amount);
    }
}

// Журнал дій у файл. Не записалось - не страшно.
public class AuditLog
{
    public static string FileName = "brightpath_audit.log";

    public void Write(string msg)
    {
        try
        {
            string path = Path.Combine(Path.GetTempPath(), FileName);
            File.AppendAllText(path, DateTime.Now.ToString("s") + " " + msg + "\n");
        }
        catch { }
    }
}
