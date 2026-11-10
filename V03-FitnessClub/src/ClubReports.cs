namespace FitnessClub;

// Reports for the club admin
public class ClubReports {
    private ClubRepository repo;

    public ClubReports(ClubRepository r) {
        repo = r;
    }

    public void PrintDailyReport() {
        int gym = 0, cls = 0;
        decimal fees = 0;
        foreach (Visit v in repo.Visits) {
            if (v.ClassId == 0)
                gym++;
            else
                cls++;
            fees += v.Fee;
        }
        int a = 0, fr = 0, ex = 0;
        foreach (Membership ms in repo.Memberships) {
            if (ms.Status == MembershipStatus.Active)
                a++;
            else if (ms.Status == MembershipStatus.Frozen)
                fr++;
            else if (ms.Status == MembershipStatus.Expired)
                ex++;
        }
        Console.WriteLine("Visits: " + repo.Visits.Count + ", gym " + gym + ", classes " + cls);
        Console.WriteLine("Class fees: " + fees + " UAH, inside now: " + VisitService.PeopleInside);
        Console.WriteLine("Memberships: active " + a + ", frozen " + fr + ", expired " + ex
            + ", cancelled " + (repo.Memberships.Count - a - fr - ex));
    }

    public void print_debtors() {
        Console.WriteLine("Debtors:");
        foreach (Member m in repo.Members) {
            if (m.Debt <= 0)
                continue;
            string line = "  #" + m.Id + " " + m.FirstName + " " + m.LastName;
            if (m.IsStudent)
                line += " (student)";
            if (m.Trainer != null)
                line += ", trainer " + m.Trainer;
            if (m.Debt > 0)
                line += ", debt " + m.Debt + " UAH";
            // понад 500 - тільки через адміністратора
            if (m.Debt > 500)
                line += " - BLOCKED";
            Console.WriteLine(line);
        }
    }

    public void PrintMemberStatement(int memberId) {
        Member m = repo.FindMember(memberId);
        if (m == null) {
            Console.WriteLine("Statement: member #" + memberId + " not found");
            return;
        }
        string line = "  #" + m.Id + " " + m.FirstName + " " + m.LastName;
        if (m.IsStudent)
            line += " (student)";
        if (m.Trainer != null)
            line += ", trainer " + m.Trainer;
        if (m.Debt > 0)
            line += ", debt " + m.Debt + " UAH";
        Console.WriteLine("Statement:" + line + ", points " + m.BonusPoints);
        Membership ms = repo.GetLatestMembership(m.Id);
        if (ms == null)
            return;
        int d = (ms.EndDate.Date - DateTime.Today).Days;
        string s = "  " + ms.Type + ", " + ms.Status + ", days left " + (d < 0 ? 0 : d);
        if (ms.Type == "Visits10")
            s += ", visits left " + ms.VisitsLeft;
        Console.WriteLine(s);
    }
}
