namespace LibraryDesk;

public class Program
{
    public static void Main(string[] args)
    {
        LibraryStore store = new LibraryStore();
        Seed(store);
        LoanManager mgr = new LoanManager(store);
        ReturnDesk desk = new ReturnDesk(store);
        Console.WriteLine("=== LibraryDesk: circulation desk demo ===");
        Console.WriteLine("Readers " + store.Readers.Count + ", books " + store.Books.Count);
        Console.WriteLine("-- Issue --");
        int first = TryIssue(mgr, "R-1001", "INV-0001", 0);
        int textbook = TryIssue(mgr, "r-1002 ", "INV-0002", 0);
        int mag = TryIssue(mgr, "R-1002", "INV-0003", 0);
        TryIssue(mgr, "R-1001", "INV-0006", 10);
        TryIssue(mgr, "R-1003", "INV-0007", 0);
        TryIssue(mgr, "R-1006", "INV-0009", 0);
        Console.WriteLine("-- Issue: refusals --");
        TryIssue(mgr, "R-1001", "INV-0004", 0);
        TryIssue(mgr, "R-1004", "INV-0005", 0);
        TryIssue(mgr, "R-1005", "INV-0005", 0);
        TryIssue(mgr, "R-1002", "INV-0001", 0);
        TryIssue(mgr, "R-9999", "INV-0005", 0);
        TryIssue(mgr, null, "INV-0005", 0);
        Console.WriteLine("-- Extend --");
        for (int i = 1; i <= 4; i++)
            print_result("Extend #" + first + " (" + i + ")", mgr.Extend(first, 14), mgr);
        print_result("Extend #" + mag + " (periodical)", mgr.Extend(mag, 14), mgr);
        print_result("Extend #1002 (overdue)", mgr.Extend(1002, 14), mgr);
        print_result("Extend #9999", mgr.Extend(9999, 14), mgr);
        Returns(store, mgr, desk, textbook);
        ReportBuilder rep = new ReportBuilder(store);
        Console.WriteLine("-- Reports --");
        Console.WriteLine(rep.ReaderSummary("R-1001"));
        Console.WriteLine(rep.ReaderSummary("R-1002"));
        Console.WriteLine(rep.ReaderSummary("R-1004"));
        Console.WriteLine(rep.ReaderSummary("R-7777"));
        Console.WriteLine("Desk: issued " + mgr.IssuedToday + ", denied " + mgr.DeniedToday
            + ", audit records " + AuditLog.Recent.Count);
    }

    static void Returns(LibraryStore store, LoanManager mgr, ReturnDesk desk, int textbook)
    {
        Console.WriteLine("-- Return --");
        Console.WriteLine("Overdue #1001: " + mgr.IsOverdue(store.FindLoan(1001))
            + ", #1003: " + mgr.IsOverdue(store.FindLoan(1003)));
        Console.WriteLine("Return #1001: fine " + desk.Return(1001).ToString("0.00"));
        try
        {
            desk.Return(1001);
        }
        catch (InvalidOperationException e)
        {
            Console.WriteLine("Return #1001 again: ERROR " + e.Message);
        }
        print_result("Extend #1001 after return", mgr.Extend(1001, 14), mgr);
        Console.WriteLine("Return #9999: " + desk.Return(9999));
        desk.MarkLost(textbook);
        desk.MarkLost(1001);
        Console.WriteLine("Loan #1001 after MarkLost: " + store.FindLoan(1001).Status);
    }

    static int TryIssue(LoanManager m, string card, string invNo, int days)
    {
        int id = m.Issue(card, invNo, days, false);
        string label = "Issue " + (card == null ? "<null>" : card.Trim()) + " / " + invNo;
        if (id < 0)
        {
            Console.WriteLine(label + ": DENIED - " + m.LastError);
            return id;
        }
        string s = label + ": loan #" + id + ", " + m.LastTerm + " days";
        if (m.LastFine > 0)
            s = s + ", current fine " + m.LastFine.ToString("0.00");
        Console.WriteLine(s);
        return id;
    }

    static void print_result(string label, bool ok, LoanManager m)
    {
        Console.WriteLine(label + (ok ? ": OK" : ": FAILED - " + m.LastError));
    }

    static void Seed(LibraryStore s)
    {
        s.AddReader("R-1001", "Olena Kovalenko", "+380501112233", "adult", 0, false);
        s.AddReader("R-1002", "Taras Melnyk", "+380671234567", "student", 0, false);
        s.AddReader("R-1003", "Iryna Bondar", "+380931112200", "adult", 0, false);
        s.AddReader("R-1004", "Petro Savchuk", "", "pensioner", 45, false);
        s.AddReader("R-1005", "Andrii Lysenko", "+380660000001", "child", 0, true);
        // імпорт зі старої системи
        s.AddReader("R-1006", "Marta Hnatiuk", "+380972223344", "Student", 0, false);
        s.AddBook("INV-0001", "Kobzar", "T. Shevchenko", "fiction", false, 250);
        s.AddBook("INV-0002", "Higher Mathematics", "P. Ovchynnykov", "textbook", false, 400);
        s.AddBook("INV-0003", "Ukrainian Week No. 12", "-", "periodical", false, 60);
        s.AddBook("INV-0004", "Encyclopedia of Ukraine, v.3", "NASU", "reference", false, 900);
        s.AddBook("INV-0005", "Eneida (1842)", "I. Kotliarevskyi", "fiction", true, 15000);
        s.AddBook("INV-0006", "Tiger Trappers", "I. Bahrianyi", "fiction", false, 180);
        s.AddBook("INV-0007", "Forest Song", "Lesia Ukrainka", "fiction", false, 150);
        s.AddBook("INV-0008", "Shadows of Forgotten Ancestors", "M. Kotsiubynskyi", "fiction",
            false, 200);
        s.AddBook("INV-0009", "Data Structures in C#", "O. Petrenko", "textbook", false, 520);
        s.AddBook("INV-0010", "Intermezzo", "M. Kotsiubynskyi", "fiction", false, 120);
        // старі видачі, прострочені на 10 і 20 днів
        OldLoan(s, "R-1003", "INV-0008", 10);
        OldLoan(s, "R-1004", "INV-0010", 20);
    }

    static void OldLoan(LibraryStore s, string card, string invNo, int daysOverdue)
    {
        s.Loans.Add(new Loan { Id = LibraryStore.NextLoanId++, ReaderCard = card,
            InventoryNo = invNo, IssuedOn = DateTime.Today.AddDays(-14 - daysOverdue),
            DueDate = DateTime.Today.AddDays(-daysOverdue) });
        s.FindBook(invNo).Available = false;
    }
}
