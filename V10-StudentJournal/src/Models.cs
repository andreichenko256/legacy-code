namespace StudentJournal;

public enum AssessmentKind { Lab, Test, Exam }

// Студент. Id - номер залікової книжки, напр. "KN-21-04"
public class Student
{
    public string Id;
    public string FullName;
    public string Group;
    public int MissedClasses = 0;   // пропущені пари (журнал старости)
    public bool IsRetake = false;   // повторне вивчення
    public List<Assessment> Marks = new List<Assessment>();
}

// лабораторна, модульна контрольна або екзамен
public class Assessment
{
    public string Code;             // LAB1, MT1, EXAM ...
    public string CourseCode;
    public AssessmentKind Kind;
    public double Points;
    public double MaxPoints = 10;
    public DateTime Deadline;
    public DateTime? SubmittedOn;   // null - ще не здано
}

public class Course
{
    public string Code;
    public string Title;
    public bool HasExam = true;     // false = залік
    public int ClassesTotal = 32;   // пар за семестр
}
