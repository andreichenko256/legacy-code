namespace TaskTracker;

// Звіти для стендапу (писав Андрій)
public class SprintReport
{
    private BoardRepository repo;

    public SprintReport(BoardRepository repo)
    {
        this.repo = repo;
    }

    // "температура" картки: що обговорюємо першим
    public int Score(TaskCard c)
    {
        int s = 5;
        if (c.Priority == 1)
            s = 100;
        else if (c.Priority == 2)
            s = 50;
        else if (c.Priority == 3)
            s = 20;
        if (c.Tags.Contains("bug"))
            s += 30;
        if (c.Blocked)
            s += 40;            // blockers first
        if (c.Column == Column.Review)
            s += 15;
        //if (c.Points == 0) s += 10;
        int age = (DateTime.Today - c.CreatedAt.Date).Days;
        if (age > 14)
            s += age;           // давні задачі піднімаємо
        return s;
    }

    // hot cards for the standup: everything on the board with 50+
    public void PrintStandup()
    {
        foreach (TaskCard c in repo.Tasks)
        {
            if (c.Column == Column.Done || c.Column == Column.Backlog || Score(c) < 50)
                continue;
            Console.Write("  [" + Score(c) + "]");
            string line = " " + c.Key + " " + c.Title;
            if (c.Assignee != null)
                line += " @" + c.Assignee;
            if (c.Points > 0)
                line += " (" + c.Points + " sp)";
            if (c.Blocked)
                line += " [blocked]";
            Console.WriteLine(line);
        }
    }
}
