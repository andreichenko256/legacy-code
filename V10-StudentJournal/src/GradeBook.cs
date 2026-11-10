namespace StudentJournal;

// Підсумкові оцінки (положення про рейтинг).
// Вага: лаби 50, модульні 20, екзамен 30.
public class GradeBook
{
    public static int CalcCount = 0;
    public static bool UseFivePointScale = false;   // old printed form (before 2019)
    private JournalRepository repo;
    public int MaxBonus = 10;

    // результати останнього Calc (GetLetter / NeedsRetake / LastInfo)
    private double _avg;
    private string _lastStudentId;
    private string _lastCourse;
    private int _late;
    private int _missed;
    private int _att;
    private bool _admitted = true;

    public GradeBook(JournalRepository repository)
    {
        repo = repository;
    }

    // Final score 0..100. strict = exam sheet (жорсткіші штрафи).
    // Повертає -1, якщо студента або дисципліни немає.
    public int Calc(string studentId, string courseCode, int bonus, bool strict)
    {
        CalcCount++;
        _lastStudentId = studentId;
        _lastCourse = courseCode;
        _avg = 0;
        _late = 0;
        _missed = 0;
        _admitted = true;
        Student st = repo.FindStudent(studentId);
        Course c = repo.FindCourse(courseCode);
        if (st == null || c == null)
        {
            Console.WriteLine("  ! not found: " + studentId + " / " + courseCode);
            return -1;
        }
        double labs = 0, labsMax = 0, tests = 0, testsMax = 0, exam = 0;
        DateTime today = DateTime.Today;
        foreach (Assessment a in st.Marks)
        {
            // Marks - оцінки з усіх дисциплін, чужі пропускаємо
            if (a.CourseCode != courseCode)
                continue;
            if (a.Kind == AssessmentKind.Exam)
            {
                exam = a.Points;   // retake overwrites the first attempt
                continue;
            }
            if (a.Kind == AssessmentKind.Test)
            {
                tests += a.Points;
                testsMax += a.MaxPoints;
                continue;
            }
            double p = a.Points;
            if (a.SubmittedOn == null)
            {
                if (a.Deadline.Date >= today)
                    continue;   // термін ще не минув
                p = 0;
                _missed++;
            }
            else
            {
                int d = (a.SubmittedOn.Value.Date - a.Deadline.Date).Days;
                if (d > 0)
                {
                    _late++;
                    // -10% за кожен розпочатий тиждень
                    if (strict && d > 14)
                        p = 0;
                    else
                        p = p * (1 - 0.1 * ((d + 6) / 7));
                }
            }
            labs += p;
            labsMax += a.MaxPoints;
        }
        double labPart = labsMax > 0 ? labs / labsMax : 0;
        double testPart = testsMax > 0 ? tests / testsMax : 0;
        _att = 100 - st.MissedClasses * 100 / c.ClassesTotal;
        // допуск: відвідування >= 50%, незданих лаб <= 2
        _admitted = _att >= 50 && _missed <= 2;
        if (c.HasExam)
        {
            if (!_admitted)
                exam = 0;
            _avg = labPart * 40 + testPart * 20 + exam;   // exam is already out of 40
        }
        else
        {
            _avg = labPart * 60 + testPart * 40;
        }
        //if (st.Group.StartsWith("Z"))   // заочники без бонусів
        //    bonus = 0;
        _avg += Math.Min(bonus, MaxBonus);
        if (_avg > 100)
            _avg = 100;
        // літня ліквідація заборгованості - не більше 60
        if (st.IsRetake && (today.Month == 7 || today.Month == 8))
            _avg = Math.Min(_avg, 60);
        int res = (int)Math.Round(_avg, MidpointRounding.AwayFromZero);
        AuditLog.Write("calc " + studentId + "/" + courseCode + " = " + res);
        return res;
    }

    // ECTS для останнього розрахованого студента
    public string GetLetter()
    {
        if (UseFivePointScale)   // стара відомість: 5/4/3/2
            return _avg >= 90 ? "5" : _avg >= 75 ? "4" : _avg >= 60 ? "3" : "2";
        if (_avg >= 90) return "A";
        if (_avg >= 82) return "B";
        if (_avg >= 74) return "C";
        if (_avg >= 64) return "D";
        if (_avg >= 60) return "E";
        if (_avg >= 35) return "FX";
        return "F";
    }

    public bool NeedsRetake()
    {
        return _avg < 60 || !_admitted;   // FX теж
    }

    public string LastInfo()
    {
        string s = _lastStudentId + "/" + _lastCourse + ": late " + _late
            + ", missed " + _missed + ", attendance " + _att + "%";
        return _admitted ? s : s + ", not admitted";
    }
}
