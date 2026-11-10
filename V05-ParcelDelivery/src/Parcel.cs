namespace ParcelDelivery;

public enum ParcelStatus { Created, Accepted, InTransit, Delivered, Returned }

// Картка посилки, поля заповнює форма каси.
public class Parcel
{
    public string TrackingNo;
    public Customer Sender;
    public string RecipientName;
    public string RecipientPhone;
    public string City;
    public string Zone;             // A - місто, B - область, C - віддалені
    public string Tariff;           // STANDARD, EXPRESS, ECONOMY, ...
    public decimal WeightKg;
    public decimal DeclaredValue;   // оголошена цінність, грн
    public int Pieces = 1;
    public decimal Price;
    public ParcelStatus Status = ParcelStatus.Created;
    public DateTime StatusChangedAt;
    public List<string> History = new List<string>();

    public bool IsClosed() => Status == ParcelStatus.Delivered || Status == ParcelStatus.Returned;
}
