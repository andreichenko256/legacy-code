namespace UtilityBills;

public class BillCalculator
{
    private BillingRepository repo;
    public double LastSubsidy;       // субсидія з останнього Charge
    public double LastUnits;         // одиниць у розрахунку
    public string LastNote = "";

    public BillCalculator(BillingRepository repo)
    {
        this.repo = repo;
    }

    // Нарахування за одну послугу за місяць, грн.
    // Повертає -1, якщо особового рахунку немає.
    public double Charge(string accNo, string svc, double prev, double cur, double night,
        DateTime readAt)
    {
        LastSubsidy = 0;
        LastUnits = 0;
        LastNote = "";
        Account a = repo.FindAccount(accNo);
        if (a == null)
        {
            Console.WriteLine("  [calc] account " + accNo + " not found");
            return -1;
        }
        if (prev < 0 || cur < 0)
            return -2;
        string s = svc.Trim().ToUpper();
        double u = cur - prev;
        // перевіряємо, чи показник не застарий
        if ((DateTime.Today - readAt.Date).Days > 45)
        {
            u = NormUnits(s, a.Residents);
            night = 0;
            LastNote = "by norm";
        }
        LastUnits = u;

        double sum = 0;
        if (s == "EL")
        {
            // steps: up to 100 kWh incl. - 2.64, 100-600 - 4.32, over 600 - 6.20
            if (u <= 100)
                sum = u * 2.64;
            else if (u > 100 && u <= 600)
                sum = 100 * 2.64 + (u - 100) * 4.32;
            else
                sum = 100 * 2.64 + 500 * 4.32 + (u - 601) * 6.20;
            // електроопалення, жовтень-квітень:
            // до 3000 кВт*год - за першим ступенем
            if (a.ElectricHeating
                && (DateTime.Today.Month >= 10 || DateTime.Today.Month <= 4))
            {
                if (u <= 3000)
                    sum = u * 2.64;
                else
                    sum = 3000 * 2.64 + (u - 3000) * 4.32;
            }
            // 2Z - двозонний, ніч за пів ціни
            if (a.MeterType == "2Z" && night > 0)
            {
                double nightPart = sum * night / u;
                sum = sum - nightPart * 0.5;
            }
        }
        else if (s == "GAS")
        {
            double gas = u * 7.96;
            sum = gas + 37.50;   // + абонплата за розподіл
        }
        else
        {
            // water: cold 30.12 + sewage 21.48 per m3
            double cold = u * 30.12;
            double drain = u * 21.48;
            sum = cold + drain;
        }
        //if (s == "HEAT")
        //    sum = a.Area * 41.26;   // з 2023 рахує теплопостачальник

        // пільги: ЧАЕС -50%, УБД і багатодітні -25%
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
        LastSubsidy = Math.Round(subsidy, 2);
        sum = Math.Round(sum, 2);
        AuditLog.Write("charge " + accNo + " " + s + " units=" + u + " sum=" + sum
            + " subsidy=" + LastSubsidy);
        return sum;
    }

    // норма на місяць, якщо показник не передали
    public double NormUnits(string s, int residents)
    {
        if (s == "EL")
            return 70 + 30 * (residents - 1);
        if (s == "GAS")
            return 9.8 * residents;
        return 2.4 * residents;   // вода
    }

    // пеня 0.1% за день, не більше 10% суми
    public double calcPenalty(Bill b)
    {
        if (b.Status != BillStatus.Overdue || b.Amount <= 0)
            return 0;
        int days = (DateTime.Today - b.DueDate.Date).Days;
        double p = b.Amount * 0.001 * days;
        if (p > b.Amount * 0.1)
            p = b.Amount * 0.1;
        return Math.Round(p, 2);
    }
}
