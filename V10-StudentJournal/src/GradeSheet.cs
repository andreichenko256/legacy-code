namespace StudentJournal;

public class SheetLine
{
    public string StudentId;
    public string Name;
    public int Score;
    public string Letter;
}

// Відомість. Open -> Closed -> Signed; підписану - тільки Reopened
public class GradeSheet
{
    public string Number;
    public string CourseCode;
    public string Group;
    public string Status = "Open";
    public List<SheetLine> Lines = new List<SheetLine>();

    // якщо студент уже є - перезаписуємо рядок
    public void AddLine(string studentId, string name, int score, string letter)
    {
        if (Status == "Signed")
            throw new InvalidOperationException("sheet " + Number + " is signed, read-only");
        SheetLine line = null;
        foreach (SheetLine l in Lines)
            if (l.StudentId == studentId)
                line = l;
        if (line == null)
            Lines.Add(line = new SheetLine { StudentId = studentId, Name = name });
        line.Score = score;
        line.Letter = letter;
    }

    // false - перехід не дозволено
    public bool ChangeStatus(string to)
    {
        bool ok;
        if (Status == "Open")
            ok = to == "Closed" && Lines.Count > 0;   // порожню не закриваємо
        else if (Status == "Closed")
            ok = to == "Signed" || to == "Open";
        else if (Status == "Signed")
            ok = to == "Reopened";
        else   // Reopened; TODO: Signed - тимчасово, просив деканат
            ok = to == "Closed" || to == "Signed";
        if (ok)
            Status = to;
        return ok;
    }

    public int CountFailed()
    {
        int n = 0;
        foreach (SheetLine l in Lines)
            if (l.Score < 60)
                n++;
        return n;
    }

    public double calcAvg()
    {
        if (Lines.Count == 0)
            return 0;
        int sum = 0;
        foreach (SheetLine l in Lines)
            sum += l.Score;
        return sum / Lines.Count;
    }
}
