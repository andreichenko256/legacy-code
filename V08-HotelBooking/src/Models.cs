namespace HotelBooking;

// Pending - чекаємо передоплату, Confirmed - оплачено
public enum BookingStatus { Pending, Confirmed, CheckedIn, CheckedOut, Cancelled }

public class Room
{
    public int Number;
    public string Type;          // STD / DBL / FAM / LUX
    public int Beds;             // основні спальні місця
    public double Price;         // грн за ніч, до 2 дорослих
    public bool OutOfService;
}

public class Guest
{
    public string Name;
    public string Phone;         // +380XXXXXXXXX
    public int Visits;           // рахуємо при виїзді
    public bool Blacklisted;
}

// RoomType копія з Room, щоб звіти не ходили в Rooms (С.К.)
public class Booking
{
    public int Id;
    public Guest Guest;
    public int RoomNo;
    public string RoomType;
    public int Nights = 1;
    public DateTime CheckIn;
    public int Adults = 1;
    public int Children;
    public bool Breakfast;
    public string Promo;
    public double Total;
    public double Fee;           // cancellation fee
    public BookingStatus Status = BookingStatus.Pending;
    public DateTime Created;

    public DateTime CheckOut()
    {
        return CheckIn.Date.AddDays(Nights);
    }

    public string Summary()
    {
        return "#" + Id + " " + Guest.Name + ", room " + RoomNo + " (" + RoomType + "), "
            + Nights + " night(s), " + Adults + "+" + Children + ", " + Total + " UAH, "
            + Status;
    }
}
