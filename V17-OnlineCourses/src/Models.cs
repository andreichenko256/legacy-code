namespace OnlineCourses;

// Pending - записаний, але ще не сплатив
public enum EnrollmentStatus { Pending, Active, Completed, Dropped }

public class Course
{
    public string Code;              // "CS101"
    public string Title;
    public string Instructor;
    public decimal Price;            // UAH
    public int Seats;
    public int Weeks;
    public DateTime OpensOn;
    public DateTime ClosesOn;        // last day of enrollment
    public bool Archived;
    public List<string> Prerequisites = new List<string>();
    public List<int> Waitlist = new List<int>();   // learner ids, first come first served
}

public class Learner
{
    public int Id;
    public string Name;
    public string Email;
    public string Plan;              // FREE, PRO, CORP, STUDENT
    public decimal Balance;
    public decimal CompanyDebt;      // CORP: рахунок роботодавцю
    public List<string> CompletedCourses = new List<string>();
}

public class Enrollment
{
    public int Id, LearnerId;
    public string CourseCode;
    public EnrollmentStatus Status;
    public decimal PricePaid;
    public string PromoCode;
    public DateTime EnrolledOn, PaidOn, CompletedOn;
    public int Progress;             // % of lessons watched
    public int Score;

    // Pending -> Active -> Completed, Pending/Active -> Dropped
    // false - якщо перехід заборонений
    public bool ChangeStatus(EnrollmentStatus to)
    {
        bool ok = false;
        switch (Status)
        {
            case EnrollmentStatus.Pending:
                ok = to == EnrollmentStatus.Active || to == EnrollmentStatus.Dropped;
                break;
            case EnrollmentStatus.Active:
                ok = to == EnrollmentStatus.Completed || to == EnrollmentStatus.Dropped;
                break;
            // Completed, Dropped - кінцеві
        }
        if (!ok)
            return false;
        Status = to;
        if (to == EnrollmentStatus.Completed)
            CompletedOn = DateTime.Today;
        return true;
    }

    public override string ToString()
    {
        return "#" + Id + " " + CourseCode + " " + Status + " " + PricePaid.ToString("0.00");
    }
}
