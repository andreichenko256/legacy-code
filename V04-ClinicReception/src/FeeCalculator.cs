namespace ClinicReception;

public class FeeCalculator
{
    // тимчасово: бухгалтерія перераховує 2023 рік
    public static bool UseOldPrices = false;

    public decimal CalcFee(Appointment a, Patient p)
    {
        decimal fee;
        switch (a.VisitType)
        {
            case "CONS": fee = 450m; break;
            case "FUP": fee = 300m; break;
            case "PROC": fee = 900m; break;
            case "VACC": fee = 250m; break;
            default: fee = 450m; break;
        }
        if (UseOldPrices)
            fee = fee * 0.8m;
        if (a.Urgent)
            fee = fee * 1.25m; // націнка за терміновість
        // страхова покриває 30%, пенсіонерам (65+) ще -10%
        if (p.InsuranceNo != null && p.InsuranceNo != "")
            fee = fee * 0.7m;
        if (DateTime.Now.Year - p.BirthYear >= 65)
            fee = fee - fee * 0.1m;
        return Math.Round(fee, 2);
    }

    public string FormatInvoice(Appointment a, Patient p)
    {
        string vt;
        switch (a.VisitType)
        {
            case "CONS": vt = "consultation"; break;
            case "FUP": vt = "follow-up"; break;
            case "PROC": vt = "procedure"; break;
            case "VACC": vt = "vaccination"; break;
            default: vt = "visit"; break;
        }
        string s = "Invoice #" + a.Id + ": " + p.FullName + ", " + vt;
        if (a.Urgent)
            s = s + " (urgent)";
        return s + " - " + a.Fee.ToString("0.00") + " UAH";
    }
}
