namespace UtilityBills;

public enum BillStatus { Draft, Issued, Overdue, Paid, Cancelled }

// особовий рахунок (квартира або будинок)
public class Account
{
    public string Number;            // "UB-1001"
    public string Owner;
    public string Address;
    public int Residents;            // зареєстровано осіб
    public string Category = "STD";  // STD, VET, CHN, LRG
    public string MeterType = "1Z";  // 1Z - однозонний, 2Z - день/ніч
    public bool ElectricHeating;
    public double SubsidyCap;        // max to pay per bill with subsidy, 0 = none
}

// рахунок за одну послугу за місяць
public class Bill
{
    public int Number;
    public string AccountNo;
    public string Service;           // EL, GAS, WATER
    public double PrevReading;
    public double CurReading;
    public double Units;
    public double Amount;            // вже з пільгою та субсидією
    public double Subsidy;
    public double Penalty;
    public double PaidSum;
    public BillStatus Status = BillStatus.Draft;
    public DateTime IssuedOn;
    public DateTime DueDate;
    public DateTime PaidOn;
    public string Note = "";

    public double ToPay()
    {
        return Math.Round(Amount + Penalty - PaidSum, 2);
    }

    public bool IsOpen()
    {
        return Status == BillStatus.Issued || Status == BillStatus.Overdue;
    }
}
