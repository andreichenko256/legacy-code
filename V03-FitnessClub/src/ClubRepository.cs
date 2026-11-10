namespace FitnessClub;

// In-memory data. TODO: move to the database
public class ClubRepository {
    private static int nextId = 1;
    public List<Member> Members = new List<Member>();
    public List<Membership> Memberships = new List<Membership>();
    public List<GroupClass> Classes = new List<GroupClass>();
    public List<Visit> Visits = new List<Visit>();

    // спільний лічильник для клієнтів і абонементів
    public static int NextId() {
        return nextId++;
    }

    public Member FindMember(int id) {
        foreach (Member m in Members) {
            if (m.Id == id)
                return m;
        }
        return null;
    }

    // поточний = останній за датою початку
    public Membership GetLatestMembership(int memberId) {
        Membership last = null;
        foreach (Membership ms in Memberships) {
            if (ms.MemberId == memberId && (last == null || ms.StartDate >= last.StartDate))
                last = ms;
        }
        return last;
    }

    public GroupClass FindClass(int id) {
        GroupClass res = null;
        foreach (GroupClass c in Classes)
            if (c.Id == id)
                res = c;
        return res;
    }
}

// Front desk audit log
public static class AuditLog {
    public static string FilePath = Path.Combine(Path.GetTempPath(), "fitness_audit.log");

    public static void Write(string msg) {
        try {
            File.AppendAllText(FilePath, DateTime.Now.ToString("s") + " " + msg + "\n");
        } catch (Exception) {
        }
    }
}
