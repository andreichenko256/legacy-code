namespace StudentJournal;

// Відомості: відкрити, заповнити, закрити, підписати
public class SheetService
{
    private JournalRepository repo;
    private GradeBook book;

    public SheetService(JournalRepository repo, GradeBook book)
    {
        this.repo = repo;
        this.book = book;
    }

    public string OpenSheet(string courseCode, string group)
    {
        if (repo.FindCourse(courseCode) == null)
            return null;
        GradeSheet sh = new GradeSheet
            { Number = repo.NewSheetNumber(), CourseCode = courseCode, Group = group };
        repo.Sheets.Add(sh);
        AuditLog.Write("sheet " + sh.Number + " opened: " + courseCode + "/" + group);
        return sh.Number;
    }

    // returns number of students for retake, -1 if there is no such sheet
    public int FillSheet(string sheetNo, int bonus)
    {
        GradeSheet sh = repo.FindSheet(sheetNo);
        if (sh == null)
            return -1;
        int retakes = 0;
        foreach (Student s in repo.Students)
        {
            if (s.Group != sh.Group)
                continue;
            int score = book.Calc(s.Id, sh.CourseCode, bonus, true);
            //if (score < 0) continue;
            // GetLetter/NeedsRetake - тільки одразу після Calc!
            sh.AddLine(s.Id, s.FullName, score, book.GetLetter());
            if (book.NeedsRetake())
            {
                retakes++;
                Console.WriteLine("  retake: " + s.FullName + " (" + book.LastInfo() + ")");
            }
        }
        return retakes;
    }

    public bool CloseSheet(string sheetNo)
    {
        sheetNo = sheetNo.Trim().ToUpper();   // з форми приходить "vd-001 "
        GradeSheet sh = repo.FindSheet(sheetNo);
        if (sh == null)
            return false;
        string old = sh.Status;
        bool ok = sh.ChangeStatus("Closed");
        if (ok)
            AuditLog.Write("sheet " + sh.Number + ": " + old + " -> " + sh.Status);
        else
            Console.WriteLine("  ! " + sh.Number + ": " + old + " -> Closed is not allowed");
        return ok;
    }

    public bool SignSheet(string sheetNo)
    {
        GradeSheet sh = repo.FindSheet(sheetNo);
        if (sh == null)
            return false;
        string old = sh.Status;
        bool ok = sh.ChangeStatus("Signed");
        if (ok)
            AuditLog.Write("sheet " + sh.Number + ": " + old + " -> " + sh.Status);
        else
            Console.WriteLine("  ! " + sh.Number + ": " + old + " -> Signed is not allowed");
        return ok;
    }

    // TODO: only with the dean's permission; reason is not stored yet
    public bool ReopenSheet(string sheetNo, string reason)
    {
        GradeSheet sh = repo.FindSheet(sheetNo);
        if (sh == null)
            return false;
        string old = sh.Status;
        bool ok = sh.ChangeStatus("Reopened");
        if (ok)
            AuditLog.Write("sheet " + sh.Number + ": " + old + " -> " + sh.Status);
        else
            Console.WriteLine("  ! " + sh.Number + ": " + old + " -> Reopened is not allowed");
        return ok;
    }

    // після апеляції; тільки у відкритій відомості
    public bool CorrectGrade(string sheetNo, string id, int score, string teacher, string reason)
    {
        GradeSheet sh = repo.FindSheet(sheetNo);
        if (sh != null)
        {
            if (sh.Status == "Open" || sh.Status == "Reopened")
            {
                Student s = repo.FindStudent(id);
                if (s != null && score >= 0 && score <= 100)
                {
                    sh.AddLine(s.Id, s.FullName, score, LetterFor(score));
                    AuditLog.Write(teacher + ": " + s.Id + " = " + score + ", " + reason);
                    return true;
                }
            }
        }
        return false;
    }

    public void PrintSheet(string sheetNo, bool detailed)
    {
        GradeSheet sh = repo.FindSheet(sheetNo);
        if (sh == null)
            return;
        Console.WriteLine("Sheet " + sh.Number + " [" + sh.Status + "] " + sh.CourseCode
            + ", group " + sh.Group);
        int passed = 0;
        foreach (SheetLine l in sh.Lines)
        {
            if (l.Score >= 60)
                passed++;
            string row = "  " + l.StudentId + "  " + l.Name.PadRight(16)
                + l.Score.ToString().PadLeft(4);
            if (detailed)   // + ECTS і національна шкала
                row += "  " + l.Letter.PadRight(3)
                    + (l.Score >= 90 ? "5" : l.Score >= 74 ? "4" : l.Score >= 60 ? "3" : "2");
            Console.WriteLine(row);
        }
        Console.WriteLine("  passed " + passed + "/" + sh.Lines.Count + ", avg "
            + sh.calcAvg());
    }

    // та сама шкала, що й у GradeBook
    private static string LetterFor(int score)
    {
        if (score >= 90) return "A";
        if (score >= 82) return "B";
        if (score >= 74) return "C";
        if (score >= 64) return "D";
        if (score >= 60) return "E";
        if (score >= 35) return "FX";
        return "F";
    }
}
