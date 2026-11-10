namespace PharmacyStock;

public enum PrescriptionStatus { Issued, PartiallyDispensed, Dispensed, Expired, Cancelled }

// Рецепт ф. 1 (електронні теж вносимо вручну)
public class Prescription
{
    public string Number;
    public string PatientName;
    public string Category;          // "PENSIONER", "DISABLED" або ""
    public string Phone;
    public string MedicineCode;
    public int Quantity;             // упаковок за рецептом
    public int DispensedQty;
    public DateTime IssuedOn;
    public int ValidDays;
    public string Doctor;
    public PrescriptionStatus Status = PrescriptionStatus.Issued;
    public decimal Paid;
    public string LastPharmacist;
    public List<string> History = new List<string>();

    public bool HasBenefit()
    {
        return Category == "PENSIONER" || Category == "DISABLED";
    }

    public string MaskedPhone()
    {
        try
        {
            return "***" + Phone.Substring(Phone.Length - 4);
        }
        catch (Exception)
        {
            return "****";
        }
    }
}
