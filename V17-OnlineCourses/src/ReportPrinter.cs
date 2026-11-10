namespace OnlineCourses;

// Звіти для навчального відділу.
public class ReportPrinter
{
    public static bool ShowInstructor = false;     // only for the old faculty report
    private CourseRepository repo;

    public ReportPrinter(CourseRepository r)
    {
        repo = r;
    }

    // full = print every learner, otherwise totals only
    public void PrintRoster(string code, bool full)
    {
        Course c = repo.FindCourse(code);
        if (c == null)
        {
            Console.WriteLine("  course " + code + " not found");
            return;
        }
        Console.WriteLine("  Roster " + c.Code + " - " + c.Title);
        if (ShowInstructor)
            Console.WriteLine("  Instructor: " + c.Instructor);
        int taken = 0;
        decimal money = 0;
        foreach (Enrollment e in repo.Enrollments)
        {
            if (e.CourseCode != c.Code)
                continue;
            if (e.Status != EnrollmentStatus.Dropped)
                taken++;
            if (e.Status == EnrollmentStatus.Active || e.Status == EnrollmentStatus.Completed)
                money = money + e.PricePaid;
            if (full)
            {
                Learner l = repo.FindLearner(e.LearnerId);
                Console.WriteLine("    #" + e.Id + " " + l.Name + " <" + l.Email + "> " + e.Status);
            }
        }
        Console.WriteLine("    seats " + taken + "/" + c.Seats + ", waitlist " + c.Waitlist.Count);
        Console.WriteLine("    revenue " + money.ToString("0.00") + " UAH");
    }
}
