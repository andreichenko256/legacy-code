namespace FitnessClub;

// Продаж абонементів, заморозка, скасування
public class MembershipService {
    public string LastError;
    private ClubRepository repo;

    public MembershipService(ClubRepository r) {
        repo = r;
    }

    public Member RegisterMember(string firstName, string lastName, string phone, string email,
        bool student) {
        if (string.IsNullOrEmpty(firstName) || string.IsNullOrEmpty(lastName))
            throw new ArgumentException("First and last name are required");
        // телефон: 10 цифр, з нуля
        if (phone == null || phone.Length != 10 || !phone.StartsWith("0"))
            throw new ArgumentException("Wrong phone number: " + phone);
        Member m = new Member { FirstName = firstName, LastName = lastName, Phone = phone };
        m.Id = ClubRepository.NextId();
        m.Email = email;
        m.IsStudent = student;
        m.CardCode = "FC-" + m.Id.ToString("0000");
        m.RegisteredOn = DateTime.Today;
        repo.Members.Add(m);
        return m;
    }

    // ціна в грн: 6 міс. -10%, 12 міс. -15%, студентам ще -20%
    public decimal calcPrice(string type, int months, bool student, string promo) {
        if (months <= 0)
            return -1;
        decimal p;
        switch (type) {
            case "Unlimited": p = 1200; break;
            case "Morning": p = 700; break;
            //case "Evening": p = 800; break;
            case "Visits10": return student ? 1200 : 1500;    // пакет, без місяців
            default: p = 900; break;                         // Standard
        }
        decimal total = p * months;
        if (months >= 12)
            total = total * 0.85m;
        else if (months >= 6)
            total = total * 0.9m;
        if (student)
            total = total * 0.8m;
        if (promo == "SUMMER" && total > 1000)
            total = total - 200;
        return Math.Round(total, 0);
    }

    public Membership SellMembership(int memberId, string type, int months, bool student,
        string promo, decimal paidNow) {
        LastError = null;
        Member m = repo.FindMember(memberId);
        if (m == null) {
            LastError = "Member #" + memberId + " not found";
            return null;
        }
        if (months <= 0 || months > 24) {
            LastError = "Wrong number of months: " + months;
            return null;
        }
        if (m.Debt > 500) {
            LastError = "Debt " + m.Debt + " UAH, sale is blocked";
            return null;
        }
        Membership ms = new Membership();
        ms.Id = ClubRepository.NextId();
        ms.MemberId = m.Id;
        ms.Type = type;
        ms.StartDate = DateTime.Today;
        ms.EndDate = DateTime.Today.AddDays(30 * months);
        ms.PricePaid = calcPrice(type, months, student || m.IsStudent, promo);
        if (type == "Visits10") {
            ms.VisitsLeft = 10;
            ms.EndDate = DateTime.Today.AddDays(90);
        }
        repo.Memberships.Add(ms);
        if (paidNow < ms.PricePaid)
            m.Debt += ms.PricePaid - paidNow;
        AuditLog.Write("SALE " + ms.Id + " " + type + " x" + months + " = " + ms.PricePaid);
        return ms;
    }

    // заморозка: тільки активний, не більше 30 днів на рік
    public bool Freeze(int membershipId, int days) {
        LastError = null;
        Membership ms = null;
        foreach (Membership x in repo.Memberships)
            if (x.Id == membershipId)
                ms = x;
        if (ms == null) {
            LastError = "Membership #" + membershipId + " not found";
            return false;
        }
        if (days <= 0) {
            LastError = "Days must be positive";
            return false;
        }
        if (ms.Status != MembershipStatus.Active) {
            LastError = "Cannot freeze membership in status " + ms.Status;
            return false;
        }
        if (ms.FreezeDaysUsed + days > 30) {
            Console.WriteLine("  ! freeze limit 30 days a year, left " + (30 - ms.FreezeDaysUsed));
            LastError = "Freeze limit exceeded";
            return false;
        }
        ms.Status = MembershipStatus.Frozen;
        ms.FreezeDaysUsed += days;
        ms.EndDate = ms.EndDate.AddDays(days);
        AuditLog.Write("FREEZE " + ms.Id + " days=" + days);
        return true;
    }

    public bool Unfreeze(int membershipId) {
        LastError = null;
        Membership ms = null;
        foreach (Membership x in repo.Memberships)
            if (x.Id == membershipId)
                ms = x;
        if (ms == null) {
            LastError = "Membership #" + membershipId + " not found";
            return false;
        }
        if (ms.Status != MembershipStatus.Frozen)
            throw new InvalidOperationException("Membership #" + ms.Id + " is not frozen");
        // TODO: повертати дні, якщо розморозили раніше
        ms.Status = MembershipStatus.Active;
        AuditLog.Write("UNFREEZE " + ms.Id);
        return true;
    }

    public bool Cancel(int membershipId, string reason) {
        LastError = null;
        Membership ms = null;
        foreach (Membership x in repo.Memberships)
            if (x.Id == membershipId)
                ms = x;
        if (ms == null) {
            LastError = "Membership #" + membershipId + " not found";
            return false;
        }
        if (ms.Status == MembershipStatus.Cancelled || ms.Status == MembershipStatus.Expired)
            return false;
        ms.Status = MembershipStatus.Cancelled;
        AuditLog.Write("CANCEL " + ms.Id + " " + (reason ?? "-"));
        return true;
    }

    // nightly job: close overdue memberships
    public int ExpireOld() {
        int n = 0;
        foreach (Membership ms in repo.Memberships) {
            if (ms.Status == MembershipStatus.Active && ms.IsExpired()) {
                ms.Status = MembershipStatus.Expired;
                n++;
            }
        }
        return n;
    }
}
