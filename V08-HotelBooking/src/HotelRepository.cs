namespace HotelBooking;

// In-memory storage. TODO: move to DB (HB-40)
public class HotelRepository
{
    public static int nextId = 1;     // shared by the whole app

    public List<Room> Rooms = new List<Room>();
    public List<Booking> Bookings = new List<Booking>();
    public List<Guest> Guests = new List<Guest>();

    public HotelRepository()
    {
        Rooms.Add(new Room { Number = 101, Type = "STD", Beds = 2, Price = 1200 });
        Rooms.Add(new Room { Number = 102, Type = "STD", Beds = 2, Price = 1200 });
        Rooms.Add(new Room { Number = 103, Type = "STD", Beds = 2, Price = 1200 });
        Rooms.Add(new Room { Number = 201, Type = "DBL", Beds = 2, Price = 1500 });
        Rooms.Add(new Room { Number = 202, Type = "DBL", Beds = 3, Price = 1700 });
        Rooms.Add(new Room { Number = 301, Type = "FAM", Beds = 4, Price = 2100 });
        Rooms.Add(new Room { Number = 302, Type = "FAM", Beds = 4, Price = 2100 });
        Rooms.Add(new Room { Number = 401, Type = "LUX", Beds = 2, Price = 3500 });
        Rooms[2].OutOfService = true;   // 103 - ремонт ванної
        Guest p = new Guest { Name = "Petro Bondar", Phone = "+380501112233" };
        p.Blacklisted = true;           // мінібар і дзеркало, 2022
        Guests.Add(p);
        Guests.Add(new Guest { Name = "Iryna Melnyk", Phone = "+380671234567", Visits = 4 });
    }

    public Room GetRoom(int no)
    {
        foreach (Room r in Rooms)
            if (r.Number == no)
                return r;
        return null;
    }

    public Booking GetBooking(int id)
    {
        for (int i = 0; i < Bookings.Count; i++)
        {
            if (Bookings[i].Id == id)
                return Bookings[i];
        }
        return null;
    }

    public Guest FindGuest(string phone)
    {
        foreach (Guest g in Guests)
            if (g.Phone == phone)
                return g;
        return null;
    }

    // чи зайнятий номер на [d1; d1+n). exceptId не рахуємо
    public bool isBusy(int roomNo, DateTime d1, int n, int exceptId)
    {
        foreach (Booking b in Bookings)
        {
            if (b.Id == exceptId || b.RoomNo != roomNo)
                continue;
            if (b.Status == BookingStatus.Cancelled || b.Status == BookingStatus.CheckedOut)
                continue;
            if (d1.Date <= b.CheckOut() && b.CheckIn < d1.Date.AddDays(n))
                return true;
        }
        return false;
    }
}

// Журнал рецепції. Тимчасово в temp, поки нема /var/log
public class AuditLog
{
    public static string FilePath = Path.Combine(Path.GetTempPath(), "hotel_audit.log");

    public static void Write(string msg)
    {
        try
        {
            string line = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " " + msg;
            File.AppendAllText(FilePath, line + "\n");
        }
        catch
        {
        }
    }
}
