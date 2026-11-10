namespace StudentJournal;

public class Program
{
    static DateTime Today = DateTime.Today;

    public static void Main(string[] args)
    {
        Console.WriteLine("Student Journal 2.4 - semester grades");
        JournalRepository repo = new JournalRepository();
        Seed(repo);
        GradeBook book = new GradeBook(repo);
        Console.WriteLine("== Final scores ==");
        foreach (Student s in repo.Students)
            PrintScore(book, s.Id, "OOP-2", 0, true);
        PrintScore(book, "KN-21-02", "OOP-2", 0, false);
        PrintScore(book, "KN-21-02", "OOP-2", 5, false);
        PrintScore(book, "KN-21-02", "OOP-2", 25, false);
        PrintScore(book, "KN-21-03", "DB-1", 0, true);
        Console.WriteLine("  last: " + book.LastInfo());
        Console.WriteLine("== Errors ==");
        PrintScore(book, "KN-99-99", "OOP-2", 0, true);
        try
        {
            repo.AddMark("KN-21-01", "OOP-2", "LAB5", AssessmentKind.Lab, -5, 10, Today, Today);
        }
        catch (ArgumentException e)
        {
            Console.WriteLine("AddMark: " + e.Message);
        }
        RunSheets(repo, new SheetService(repo, book));
        Console.WriteLine("Calculations: " + GradeBook.CalcCount + ", audit: " + AuditLog.Count);
    }

    static void Seed(JournalRepository repo)
    {
        repo.Courses.Add(new Course { Code = "OOP-2", Title = "Object-Oriented Programming" });
        repo.Courses.Add(new Course { Code = "DB-1", Title = "Databases", HasExam = false });
        string csv = "id;name;group;missed\nKN-21-01;Ivanenko Olena;KN-21;1\n"
            + "KN-21-02;Petrenko Taras;KN-21;6\nKN-21-03;Shevchuk Iryna;KN-21;18\n"
            + "KN-21-04;Bondar Maksym;KN-21;3\nKN-21-05;Melnyk Andrii;KN-21\n";
        Console.WriteLine("Imported students: " + repo.ImportStudents(csv));
        // labs "points:days after deadline" ("-" = not submitted), then MT1, MT2, exam
        AddMarks(repo, "KN-21-01", "OOP-2", "10:-2 9:0 10:-1", 18, 19, 36);
        AddMarks(repo, "KN-21-02", "OOP-2", "8:0 9:5 10:16", 14, 13, 28);
        AddMarks(repo, "KN-21-03", "OOP-2", "6:0 0:- 0:-", 10, 8, 20);
        AddMarks(repo, "KN-21-04", "OOP-2", "9:0 9:-3 10:0", 16, 17, 36);
        AddMarks(repo, "KN-21-03", "DB-1", "7:3 0:-", 12, 10, -1);
    }

    // дедлайни: 60, 40, 20 днів тому; наступна лаба - через 10
    static void AddMarks(JournalRepository repo, string id, string course, string labs,
        double mt1, double mt2, double exam)
    {
        string[] items = labs.Split(' ');
        for (int i = 0; i < items.Length; i++)
        {
            string[] p = items[i].Split(':');
            DateTime dl = Today.AddDays(-60 + 20 * i);
            DateTime? sub = p[1] == "-" ? null : dl.AddDays(int.Parse(p[1]));
            repo.AddMark(id, course, "LAB" + (i + 1), AssessmentKind.Lab, double.Parse(p[0]), 10,
                dl, sub);
        }
        repo.AddMark(id, course, "LAB" + (items.Length + 1), AssessmentKind.Lab, 0, 10,
            Today.AddDays(10), null);
        repo.AddMark(id, course, "MT1", AssessmentKind.Test, mt1, 20, Today, Today);
        repo.AddMark(id, course, "MT2", AssessmentKind.Test, mt2, 20, Today, Today);
        if (exam >= 0)
            repo.AddMark(id, course, "EXAM", AssessmentKind.Exam, exam, 40, Today, Today);
    }

    static void PrintScore(GradeBook book, string id, string course, int bonus, bool strict)
    {
        int score = book.Calc(id, course, bonus, strict);
        Console.WriteLine("  " + id + "  " + course.PadRight(6) + (strict ? "strict" : "soft  ")
            + "  bonus " + bonus.ToString().PadLeft(2) + "  ->" + score.ToString().PadLeft(4)
            + "  " + book.GetLetter());
    }

    static void RunSheets(JournalRepository repo, SheetService svc)
    {
        Console.WriteLine("== Grade sheets ==");
        string no = svc.OpenSheet("OOP-2", "KN-21");
        Console.WriteLine("Opened " + no);
        Console.WriteLine("Retakes: " + svc.FillSheet(no, 0));
        svc.PrintSheet(no, true);
        Console.WriteLine("Close: " + svc.CloseSheet(no) + ", sign: " + svc.SignSheet(no));
        try
        {
            repo.FindSheet(no).AddLine("KN-21-03", "Shevchuk Iryna", 60, "E");
        }
        catch (InvalidOperationException e)
        {
            Console.WriteLine("Add line: " + e.Message);
        }
        Console.WriteLine("Correct signed: " + svc.CorrectGrade(no, "KN-21-03", 60, "VH", "-"));
        Console.WriteLine("Reopen: " + svc.ReopenSheet(no, "appeal 14/2"));
        Console.WriteLine("Correct: " + svc.CorrectGrade(no, "KN-21-03", 60, "VH", "14/2")
            + ", to 120: " + svc.CorrectGrade(no, "KN-21-02", 120, "VH", "14/2"));
        Console.WriteLine("Sign reopened: " + svc.SignSheet(no));
        svc.PrintSheet(no, false);
        string no2 = svc.OpenSheet("DB-1", "KN-21");
        Console.WriteLine("Sign open " + no2 + ": " + svc.SignSheet(no2));
        Console.WriteLine("Close empty " + no2 + ": " + svc.CloseSheet(no2));
        Console.WriteLine("Open PHY-1: " + (svc.OpenSheet("PHY-1", "KN-21") ?? "null"));
        Console.WriteLine("Failed in " + no + ": " + repo.FindSheet(no).CountFailed());
    }
}
