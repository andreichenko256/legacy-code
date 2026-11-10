namespace OnlineCourses;

// Сховище в пам'яті. TODO: перенести в БД (планували у 2023)
public class CourseRepository
{
    public static int NextId = 1000;     // shared by all instances
    public List<Course> Courses = new List<Course>();
    public List<Learner> Learners = new List<Learner>();
    public List<Enrollment> Enrollments = new List<Enrollment>();

    public CourseRepository()
    {
        // вікно запису - у днях від сьогодні
        AddCourse("CS101", "Intro to Programming", "O. Hrytsenko", 120m, 30, 6, -10, 20);
        AddCourse("CS201", "Data Structures", "O. Hrytsenko", 180m, 25, 8, -5, 10);
        AddCourse("DS301", "Machine Learning Basics", "V. Marchenko", 250m, 2, 10, -20, 5);
        AddCourse("WEB110", "HTML and CSS", "I. Savchuk", 90m, 40, 4, -30, 0);
        AddCourse("UX120", "UX Fundamentals", "N. Petrenko", 150m, 20, 5, -30, -2);
        AddCourse("PY150", "Python for Analysts", "V. Marchenko", 160m, 20, 6, 7, 30);
        FindCourse("CS201").Prerequisites.Add("CS101");
        FindCourse("DS301").Prerequisites.Add("CS101");
        AddLearner(1, "Anna Kovalenko", "anna.k@example.com", "FREE", 300m);
        AddLearner(2, "Bohdan Melnyk", "b.melnyk@example.com", "PRO", 500m);
        AddLearner(3, "Olena Shevchuk", "olena.sh@example.com", "STUDENT", 40m);
        AddLearner(4, "Taras Bondar", "t.bondar@example.com", "CORP", 0m);
        AddLearner(5, "Iryna Tkachenko", "iryna.t@example.com", "FREE", 1000m);
        AddLearner(6, "Maksym Lysenko", "m.lysenko@example.com", "PRO", 200m);
        // CS101 пройшли ще на старій платформі
        Learners[1].CompletedCourses.Add("CS101");
        Learners[4].CompletedCourses.Add("CS101");
        Learners[5].CompletedCourses.Add("CS101");
        AddEnrollment(2, "DS301", 207m, 12, 40);
        AddEnrollment(6, "DS301", 207m, 3, 10);
        AddEnrollment(4, "CS101", 102m, 9, 85);
    }

    public void AddCourse(string code, string title, string instructor, decimal price,
        int seats, int weeks, int opensInDays, int closesInDays)
    {
        Course c = new Course();
        c.Code = code;
        c.Title = title;
        c.Instructor = instructor;
        c.Price = price;
        c.Seats = seats;
        c.Weeks = weeks;
        c.OpensOn = DateTime.Today.AddDays(opensInDays);
        c.ClosesOn = DateTime.Today.AddDays(closesInDays);
        Courses.Add(c);
    }

    public void AddLearner(int id, string name, string email, string plan, decimal balance)
    {
        Learner l = new Learner();
        l.Id = id;
        l.Name = name;
        l.Email = email;
        l.Plan = plan;
        l.Balance = balance;
        Learners.Add(l);
    }

    // запис, оплачений раніше (імпорт зі старої системи)
    public void AddEnrollment(int learnerId, string code, decimal paid, int daysAgo, int progress)
    {
        Enrollment e = new Enrollment();
        e.Id = NextId++;
        e.LearnerId = learnerId;
        e.CourseCode = code;
        e.PricePaid = paid;
        e.Status = EnrollmentStatus.Active;
        e.EnrolledOn = DateTime.Today.AddDays(-daysAgo);
        e.Progress = progress;
        Enrollments.Add(e);
    }

    public Course FindCourse(string code)
    {
        string k = code.Trim().ToUpper();
        foreach (Course c in Courses)
            if (c.Code == k)
                return c;
        return null;
    }

    public Learner FindLearner(int id)
    {
        foreach (Learner l in Learners)
            if (l.Id == id)
                return l;
        return null;
    }
}
