namespace TaskTracker;

public enum Column { Backlog, Todo, InProgress, Review, Done }

// Картка на дошці; поля публічні - так простіше з UI
public class TaskCard
{
    public int Id;
    public string Key;              // "TT-101"
    public string Title;
    public Column Column = Column.Backlog;
    public string Assignee;         // логін; null - нічия
    public int Points;              // story points
    public int Priority = 3;        // 1 - critical ... 4 - low
    public string Tags = "";        // "bug,ui"
    public bool Blocked;
    public string BlockReason;
    public string Reporter;
    public DateTime CreatedAt;
    public DateTime StartedAt;
    public DateTime DoneAt;         // MinValue - ще не закрита
}

public class User
{
    public string Login;            // завжди lower case
    public string Name;
    public string Role;             // "lead", "dev", "qa"
    public bool Active = true;
}

// історія переміщень (для звіту cycle time)
public class HistoryEntry
{
    public int TaskId;
    public Column From;
    public Column To;
    public string By;
    public DateTime At;
}
