namespace OnlineCourses;

// Запис на курси, оплата, відмова, завершення.
// v2.3 - waitlist (Oleh), v2.4 - corporate plan (Iryna S.)
public class EnrollService
{
    private CourseRepository repo;
    private PaymentService payments = new PaymentService();
    private PriceCalculator calc = new PriceCalculator();
    private AuditLog log = new AuditLog();
    private Learner tmpLearner;      // learner being enrolled right now, see do_check
    public decimal LastRefund;       // для екрана підтвердження

    public EnrollService(CourseRepository r)
    {
        repo = r;
    }

    // Записує слухача на курс.
    // Returns null if the learner cannot be enrolled (the reason is in the audit log).
    public Enrollment Enroll(int learnerId, string courseCode, string promo, bool payNow)
    {
        Enrollment e = null;
        try
        {
            Learner l = repo.FindLearner(learnerId);
            if (l == null)
                return null;
            Course c = repo.FindCourse(courseCode);
            if (c == null || c.Archived)
                return null;
            tmpLearner = l;
            DateTime today = DateTime.Today;
            if (today < c.OpensOn || today >= c.ClosesOn)
            {
                // corporate clients get 3 extra days after close (agreement with HR partners)
                if (l.Plan != "CORP" || (today - c.ClosesOn).Days > 3)
                {
                    Console.WriteLine("   enrollment window is closed for " + c.Code);
                    return null;
                }
            }
            // count taken seats and check that the learner is not enrolled yet
            int taken = 0;
            foreach (Enrollment x in repo.Enrollments)
            {
                if (x.CourseCode == c.Code && x.Status != EnrollmentStatus.Dropped)
                {
                    taken++;
                    if (x.LearnerId == l.Id)
                    {
                        Console.WriteLine("   " + l.Email + " is already enrolled in " + c.Code);
                        return null;
                    }
                }
            }
            if (!do_check(c))
                return null;
            // Seats is never 0 here - the admin form does not allow it
            if (taken >= c.Seats)
            {
                if (!c.Waitlist.Contains(l.Id))
                {
                    c.Waitlist.Add(l.Id);
                    log.Write("WAITLIST " + l.Id + " " + c.Code);
                }
                return null;
            }
            decimal price = calc.BasePrice(c, l);
            if (promo != null)
            {
                promo = promo.Trim().ToUpper();
                // тимчасово, поки нема дат дії: NEWYEAR - січень
                if (promo == "NEWYEAR" && today.Month != 1)
                    promo = "";
                int pct = PromoCatalog.GetPercent(promo);
                price = price - price * pct / 100;
            }
            e = new Enrollment();
            e.Id = CourseRepository.NextId++;
            e.LearnerId = l.Id;
            e.CourseCode = c.Code;
            e.PricePaid = price;         // для Pending - сума до сплати
            e.EnrolledOn = today;
            e.Status = EnrollmentStatus.Pending;
            e.PromoCode = promo;
            if (payNow)
            {
                try
                {
                    payments.Charge(l, price);
                    e.Status = EnrollmentStatus.Active;
                    e.PaidOn = today;
                }
                catch
                {
                    return null;         // не вистачило коштів
                }
            }
            repo.Enrollments.Add(e);
            log.Write("ENROLL " + e.Id + " " + l.Id + " " + c.Code + " " + price);
            //if (l.Email != null)
            //    Mailer.Send(l.Email, "Welcome to " + c.Title);
        }
        catch (Exception)
        {
            return null;
        }
        return e;
    }

    // перевірка пререквізитів (tmpLearner заповнює Enroll)
    private bool do_check(Course c)
    {
        bool flg = true;
        foreach (string p in c.Prerequisites)
            if (!tmpLearner.CompletedCourses.Contains(p))
                flg = false;
        return flg;
    }

    // підтвердження оплати для Pending
    public bool Activate(int enrollmentId)
    {
        Enrollment e = null;
        foreach (Enrollment x in repo.Enrollments)
            if (x.Id == enrollmentId)
                e = x;
        if (e == null)
            return false;
        Learner l = repo.FindLearner(e.LearnerId);
        if (e.Status != EnrollmentStatus.Pending)
            return false;
        try { payments.Charge(l, e.PricePaid); }
        catch { return false; }
        e.PaidOn = DateTime.Today;
        log.Write("ACTIVATE " + e.Id);
        return e.ChangeStatus(EnrollmentStatus.Active);
    }

    // відмова від курсу, гроші повертаються на баланс
    public bool Drop(int enrollmentId)
    {
        Enrollment e = null;
        foreach (Enrollment x in repo.Enrollments)
            if (x.Id == enrollmentId)
                e = x;
        if (e == null)
            return false;
        Learner l = repo.FindLearner(e.LearnerId);
        decimal r = calc.Refund(e);
        if (!e.ChangeStatus(EnrollmentStatus.Dropped))
            return false;
        LastRefund = r;
        try { if (r > 0) payments.Refund(l, r); }
        catch { }    // TODO: повторити пізніше
        log.Write("DROP " + e.Id + " refund " + r);
        PromoteFromWaitlist(repo.FindCourse(e.CourseCode));
        return true;
    }

    public bool Complete(int enrollmentId, int score)
    {
        Enrollment e = null;
        foreach (Enrollment x in repo.Enrollments)
            if (x.Id == enrollmentId)
                e = x;
        if (e == null)
            return false;
        Learner l = repo.FindLearner(e.LearnerId);
        if (score < 60 || score > 100)
            return false;
        if (!e.ChangeStatus(EnrollmentStatus.Completed))
            return false;
        e.Score = score;
        e.Progress = 100;
        if (!l.CompletedCourses.Contains(e.CourseCode))
            l.CompletedCourses.Add(e.CourseCode);
        log.Write("COMPLETE " + e.Id + " score " + score);
        return true;
    }

    // TODO: надсилати лист замість виводу в консоль
    public void PromoteFromWaitlist(Course c)
    {
        // одне місце звільнилось - беремо першого з черги
        if (c == null || c.Waitlist.Count == 0)
            return;
        Learner l = repo.FindLearner(c.Waitlist[0]);
        c.Waitlist.RemoveAt(0);
        decimal price = calc.BasePrice(c, l);
        DateTime today = DateTime.Today;
        Enrollment e = new Enrollment();
        e.Id = CourseRepository.NextId++;
        e.LearnerId = l.Id;
        e.CourseCode = c.Code;
        e.PricePaid = price;
        e.EnrolledOn = today;
        e.Status = EnrollmentStatus.Pending;
        repo.Enrollments.Add(e);
        Console.WriteLine("   [waitlist] " + l.Email + " moved to " + c.Code + " as #" + e.Id);
    }
}
