namespace FitnessClub;

// Демо-день рецепції ("ручний тест" перед релізом)
public class Program {
    public static void Main(string[] args) {
        ClubRepository repo = new ClubRepository();
        MembershipService svc = new MembershipService(repo);
        VisitService desk = new VisitService(repo);
        Console.WriteLine("=== Pulse fitness club: front desk day ===");
        Seed(repo, svc);
        Console.WriteLine("--- Sales");
        Sell(svc, 1, "Unlimited", 12, 12240);
        Sell(svc, 2, "Standard", 1, 900);
        Sell(svc, 7, "Unlimited", 3, 3200);
        Sell(svc, 9, "Platinum", 1, 900);
        Sell(svc, 8, "Morning", 1, 700);
        Sell(svc, 2, "Standard", 0, 0);
        Console.WriteLine("Unlimited x6 student: " + svc.calcPrice("Unlimited", 6, true, null));
        Console.WriteLine("Standard x0: " + svc.calcPrice("Standard", 0, false, null));
        Console.WriteLine("--- Front desk");
        // пари: клієнт, заняття (0 - тренажерний зал)
        int[] queue = { 1, 0, 1, 101, 2, 102, 2, 102, 2, 101, 3, 0, 3, 0, 3, 0,
            4, 0, 5, 0, 6, 0, 8, 0, 10, 0, 99, 0, 7, 999 };
        for (int i = 0; i < queue.Length; i += 2)
            TryIn(desk, repo, queue[i], queue[i + 1], false);
        TryIn(desk, repo, 8, 103, true);
        Console.WriteLine("Card FC-0009: " + desk.CheckInByCard("FC-0009", 0));
        Console.WriteLine("Card 12AB: " + desk.CheckInByCard("12AB", 0) + " " + desk.LastMessage);
        RunStatusChanges(repo, svc, desk);
        Console.WriteLine("--- Reports");
        ClubReports reports = new ClubReports(repo);
        reports.PrintDailyReport();
        reports.print_debtors();
        reports.PrintMemberStatement(3);
        reports.PrintMemberStatement(6);
    }

    static void Seed(ClubRepository repo, MembershipService svc) {
        svc.RegisterMember("Olena", "Kovalenko", "0671112233", "olena.k@ukr.net", false);
        svc.RegisterMember("Taras", "Melnyk", "0502223344", null, false);
        svc.RegisterMember("Iryna", "Bondar", "0933334455", "bondar.i@gmail.com", true);
        svc.RegisterMember("Andrii", "Shevchenko", "0674445566", null, false);
        svc.RegisterMember("Natalia", "Hrytsenko", "0985556677", null, false);
        svc.RegisterMember("Dmytro", "Lysenko", "0636667788", "dlysenko@ukr.net", false);
        svc.RegisterMember("Sofia", "Tkachenko", "0957778899", null, false);
        svc.RegisterMember("Maksym", "Romanenko", "0668889900", null, false);
        svc.RegisterMember("Oksana", "Kravets", "0679990011", null, false);
        svc.RegisterMember("Petro", "Savchuk", "0961234567", null, false);
        repo.FindMember(1).Trainer = "Kateryna";
        repo.FindMember(4).Debt = 650;
        repo.FindMember(8).Debt = 520;
        // temporary: old memberships from the Excel sheet
        Import(repo, 3, "Visits10", 40, 1);
        Import(repo, 4, "Morning", 20, 0);
        Import(repo, 5, "Standard", -3, 0);
        Import(repo, 6, "Unlimited", 0, 0);
        Import(repo, 8, "Standard", 15, 0);
        repo.Classes.Add(new GroupClass { Id = 101, Name = "Yoga", Capacity = 2 });
        repo.Classes.Add(new GroupClass { Id = 102, Name = "Pilates", Capacity = 10 });
        repo.Classes.Add(new GroupClass { Id = 103, Name = "CrossFit", Capacity = 8 });
        repo.FindClass(101).Booked.Add(6);
    }

    static void Import(ClubRepository repo, int memberId, string type, int daysLeft, int visits) {
        Membership m = new Membership { Id = ClubRepository.NextId(), MemberId = memberId };
        m.Type = type;
        m.StartDate = DateTime.Today.AddDays(daysLeft - 30);
        m.EndDate = DateTime.Today.AddDays(daysLeft);
        m.VisitsLeft = visits;
        repo.Memberships.Add(m);
    }

    static void Sell(MembershipService svc, int memberId, string type, int months, decimal paid) {
        Membership ms = svc.SellMembership(memberId, type, months, false, null, paid);
        string what = type + " x" + months + " to #" + memberId;
        if (ms == null)
            Console.WriteLine("Sale " + what + " failed: " + svc.LastError);
        else
            Console.WriteLine("Sold " + what + ": " + ms.PricePaid + " UAH, paid " + paid);
    }

    static void TryIn(VisitService desk, ClubRepository repo, int memberId, int classId, bool vip) {
        Member m = repo.FindMember(memberId);
        GroupClass gc = repo.FindClass(classId);
        string who = m != null ? m.FullName() : "#" + memberId;
        string where = classId == 0 ? "gym" : (gc != null ? gc.Name : "class #" + classId);
        if (vip)
            where += " (staff override)";
        int r = desk.CheckIn(memberId, classId, vip, "MAIN");
        string verdict = r > 0 ? "OK, visit " + r : "DENIED " + r;
        Console.WriteLine(who + " -> " + where + ": " + verdict + ". " + desk.LastMessage);
    }

    static void RunStatusChanges(ClubRepository repo, MembershipService svc, VisitService desk) {
        Console.WriteLine("--- Membership status");
        Console.WriteLine("Closed as expired: " + svc.ExpireOld());
        Membership sofia = repo.GetLatestMembership(7);
        Show("Freeze Sofia 14 days", svc.Freeze(sofia.Id, 14), svc, sofia);
        TryIn(desk, repo, 7, 0, false);
        Show("Unfreeze Sofia", svc.Unfreeze(sofia.Id), svc, sofia);
        TryIn(desk, repo, 7, 102, false);
        try {
            svc.Unfreeze(sofia.Id);
        } catch (InvalidOperationException ex) {
            Console.WriteLine("Unfreeze Sofia again: ERROR " + ex.Message);
        }
        Membership natalia = repo.GetLatestMembership(5);
        Show("Freeze Natalia", svc.Freeze(natalia.Id, 7), svc, natalia);
        Membership olena = repo.GetLatestMembership(1);
        Show("Freeze Olena 0 days", svc.Freeze(olena.Id, 0), svc, olena);
        Show("Freeze Olena 45 days", svc.Freeze(olena.Id, 45), svc, olena);
        Membership taras = repo.GetLatestMembership(2);
        Show("Cancel Taras", svc.Cancel(taras.Id, "moved to Lviv"), svc, taras);
        Show("Cancel Taras again", svc.Cancel(taras.Id, null), svc, taras);
        TryIn(desk, repo, 2, 0, false);
    }

    static void Show(string what, bool ok, MembershipService svc, Membership ms) {
        string st = ms == null ? "-" : ms.Status.ToString();
        if (ok)
            Console.WriteLine(what + ": OK, status " + st);
        else
            Console.WriteLine(what + ": refused (" + (svc.LastError ?? "no message") + "), " + st);
    }
}
