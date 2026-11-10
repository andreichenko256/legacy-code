namespace TaskTracker;

// Сховище в пам'яті; демо-дані - дошка команди Falcon
public class BoardRepository
{
    public static int nextId = 111;     // 101-110 - демо-картки
    public List<TaskCard> Tasks = new List<TaskCard>();
    public List<HistoryEntry> History = new List<HistoryEntry>();

    public BoardRepository()
    {
        Add(101, "Login page returns 500", Column.InProgress, "ivan", 3, 1, "bug,auth", 6);
        Add(102, "Export board to CSV", Column.Todo, null, 5, 3, "feature,export", 10);
        Add(103, "Dark theme for settings", Column.Backlog, null, 2, 4, "ui", 20);
        Add(104, "Build agents on .NET 8", Column.InProgress, "taras", 3, 2, "build,ci", 12);
        Add(105, "Typo in welcome email", Column.Review, "ivan", 1, 4, "bug,mail", 4);
        TaskCard t = Add(106, "Payment webhook retries", Column.Todo, null, 8, 2, "pay", 15);
        t.Blocked = true;
        t.BlockReason = "waiting for bank sandbox keys";
        Add(107, "Search by tag", Column.InProgress, "taras", 5, 3, "feature,search", 7);
        Add(108, "Crash on avatar upload", Column.Review, "taras", 2, 1, "bug,ui", 5);
        t = Add(109, "Onboarding checklist", Column.Done, "olena", 3, 3, "docs", 10);
        t.DoneAt = DateTime.Today.AddDays(-2);
        Add(110, "Rate limit for public API", Column.Backlog, null, 5, 2, "feature,api", 4);
    }

    public TaskCard Add(int id, string title, Column col, string who, int sp, int prio,
        string tags, int age)
    {
        TaskCard t = new TaskCard();
        t.Id = id;
        t.Key = "TT-" + id;
        t.Title = title;
        t.Column = col;
        t.Assignee = who;
        t.Points = sp;
        t.Priority = prio;
        t.Tags = tags;
        t.Reporter = "olena";
        t.CreatedAt = DateTime.Today.AddDays(-age);
        Tasks.Add(t);
        return t;
    }

    public TaskCard FindTask(int id)
    {
        foreach (TaskCard t in Tasks)
            if (t.Id == id)
                return t;
        return null;
    }
}
