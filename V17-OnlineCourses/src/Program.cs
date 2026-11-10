namespace OnlineCourses;

// Демо-прогін для підтримки: щоразу ті самі кроки.
public class Program
{
    public static void Main(string[] args)
    {
        CourseRepository repo = new CourseRepository();
        EnrollService svc = new EnrollService(repo);
        ReportPrinter printer = new ReportPrinter(repo);
        Console.WriteLine("=== BrightPath Online School: enrollment run ===");
        EnrollDemo(svc);
        Console.WriteLine("-- Status changes");
        Done("activate #1004 (Bohdan pays for CS201)", svc.Activate(1004));
        Done("activate #1004 once again", svc.Activate(1004));
        Done("complete #1002 (Taras, CS101, score 92)", svc.Complete(1002, 92));
        bool ok = svc.Drop(1001);
        Done("drop #1001 (Maksym, DS301), refund " + svc.LastRefund.ToString("0.00"), ok);
        Done("complete #1001 after drop, score 90", svc.Complete(1001, 90));
        Done("complete #1003 with score -5", svc.Complete(1003, -5));
        Done("drop #9999", svc.Drop(9999));
        Console.WriteLine("-- Wallets");
        try { new PaymentService().Charge(repo.FindLearner(1), -50m); }
        catch (ArgumentException ex) { Console.WriteLine(" charge -50 to Anna: " + ex.Message); }
        foreach (Learner l in repo.Learners)
            Console.WriteLine(" " + l.Name + " [" + l.Plan + "] balance "
                + l.Balance.ToString("0.00") + ", invoiced " + l.CompanyDebt.ToString("0.00"));
        Console.WriteLine("-- Reports");
        printer.PrintRoster("DS301", true);
        printer.PrintRoster("cs201", false);
        Console.WriteLine("payment transactions: " + PaymentService.TxCount);
        Console.WriteLine("=== end of run ===");
    }

    static void EnrollDemo(EnrollService svc)
    {
        Console.WriteLine("-- Enrollment");
        Show("Anna -> CS101, WELCOME10, pay now", svc.Enroll(1, "CS101", "WELCOME10", true));
        Show("Bohdan -> CS201, spring20, pay later", svc.Enroll(2, "CS201", "spring20", false));
        Show("Olena -> CS201 without CS101", svc.Enroll(3, "CS201", null, false));
        Show("Anna -> ' cs101 ' once again", svc.Enroll(1, " cs101 ", null, true));
        Show("Anna -> XX999", svc.Enroll(1, "XX999", "", false));
        Show("learner #42 -> CS101", svc.Enroll(42, "CS101", null, false));
        Show("Iryna -> DS301 (2 seats, both taken)", svc.Enroll(5, "DS301", null, true));
        Show("Anna -> WEB110 (closes today)", svc.Enroll(1, "WEB110", "", true));
        Show("Taras, CORP -> UX120 (closed 2 days ago)", svc.Enroll(4, "UX120", null, true));
        Show("Taras, CORP -> PY150 (opens in 7 days)", svc.Enroll(4, "PY150", "", false));
        Show("Iryna -> CS201, BLACKFRIDAY", svc.Enroll(5, "CS201", "BLACKFRIDAY", true));
        Show("Olena -> CS101, pay now, balance 40", svc.Enroll(3, "CS101", null, true));
        Show("Anna -> course code null", svc.Enroll(1, null, null, false));
    }

    static void Show(string what, Enrollment e)
    {
        string res = e == null ? "enrollment failed, please try again later" : "OK " + e;
        Console.WriteLine(" " + what + ": " + res);
    }

    static void Done(string what, bool ok)
    {
        Console.WriteLine(" " + what + ": " + (ok ? "done" : "operation failed"));
    }
}
