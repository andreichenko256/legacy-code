namespace StudentJournal;

// In-memory storage. Students come from the dean's office CSV export.
public class JournalRepository
{
    public static int NextSheetNo = 1;
    public List<Student> Students = new List<Student>();
    public List<Course> Courses = new List<Course>();
    public List<GradeSheet> Sheets = new List<GradeSheet>();

    public Student FindStudent(string id)
    {
        foreach (Student s in Students)
            if (s.Id == id)
                return s;
        return null;
    }

    public Course FindCourse(string code)
    {
        for (int i = 0; i < Courses.Count; i++)
            if (Courses[i].Code == code)
                return Courses[i];
        return null;
    }

    public GradeSheet FindSheet(string number)
    {
        int i = 0;
        while (i < Sheets.Count && Sheets[i].Number != number)
            i++;
        return i < Sheets.Count ? Sheets[i] : null;
    }

    // VD-001, VD-002 ... один лічильник на всю програму
    public string NewSheetNumber()
    {
        return "VD-" + (NextSheetNo++).ToString("000");
    }

    // TODO: check that courseCode exists
    public void AddMark(string studentId, string courseCode, string code, AssessmentKind kind,
        double points, double max, DateTime deadline, DateTime? submitted)
    {
        Student s = FindStudent(studentId);
        if (s == null)
            throw new ArgumentException("unknown student " + studentId);
        if (points < 0 || points > max)
            throw new ArgumentException("invalid points " + points + " for " + code);
        s.Marks.Add(new Assessment { Code = code, CourseCode = courseCode, Kind = kind,
            Points = points, MaxPoints = max, Deadline = deadline, SubmittedOn = submitted });
    }

    // id;ПІБ;група;пропуски - повертає кількість доданих
    public int ImportStudents(string text)
    {
        int added = 0;
        foreach (string row in text.Split('\n'))
        {
            if (row.Trim() == "" || row.StartsWith("#"))
                continue;
            try
            {
                string[] f = row.Trim().Split(';');
                Students.Add(new Student { Id = f[0], FullName = f[1], Group = f[2],
                    MissedClasses = int.Parse(f[3]) });
                added++;
            }
            catch (Exception)
            {
                // кривий рядок просто пропускаємо
            }
        }
        return added;
    }
}
