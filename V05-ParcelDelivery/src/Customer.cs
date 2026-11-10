namespace ParcelDelivery;

// Sender card (filled at the front desk)
public class Customer
{
    public string Name;
    public string Phone;
    public bool IsRegular;
    public int ParcelsSent;

    public Customer(string name, string phone, bool isRegular)
    {
        Name = name;
        Phone = phone;
        IsRegular = isRegular;
    }

    // для чека показуємо лише останні 4 цифри
    public string MaskedPhone()
    {
        try { return "***" + Phone.Substring(Phone.Length - 4); }
        catch (Exception) { return "****"; }
    }
}
