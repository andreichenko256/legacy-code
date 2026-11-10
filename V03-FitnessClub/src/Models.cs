namespace FitnessClub;

public enum MembershipStatus { Active, Frozen, Expired, Cancelled }

// Клієнт клубу
public class Member {
    public int Id;
    public string FirstName;
    public string LastName;
    public string Phone;            // 0XXXXXXXXX
    public string Email;
    public string CardCode = "";    // FC-0001
    public bool IsStudent;
    public decimal Debt;            // грн
    public DateTime RegisteredOn;
    public string Trainer;          // персональний тренер
    public int BonusPoints = 0;

    public string FullName() {
        return FirstName + " " + LastName;
    }
}

// Абонемент. Type: Standard, Unlimited, Morning, Visits10
public class Membership {
    public int Id;
    public int MemberId;
    public string Type;
    public MembershipStatus Status = MembershipStatus.Active;
    public DateTime StartDate;
    public DateTime EndDate;
    public int VisitsLeft;          // тільки Visits10
    public int FreezeDaysUsed;
    public decimal PricePaid;

    public bool IsExpired() {
        return EndDate.Date < DateTime.Today;
    }
}

public class GroupClass {
    public int Id;
    public string Name;
    public int Capacity;
    public List<int> Booked = new List<int>();     // Id клієнтів
}

public class Visit {
    public int Id { get; set; }
    public int MemberId { get; set; }
    public int ClassId { get; set; }                // 0 - зал
    public decimal Fee { get; set; }
    public string Desk { get; set; }
}
