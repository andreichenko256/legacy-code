namespace TaskTracker;

public class Program
{
    static BoardRepository repo = new BoardRepository();
    static TaskBoard board = new TaskBoard(repo);

    public static void Main(string[] args)
    {
        Console.WriteLine("=== Falcon team board ===");
        Console.WriteLine("tasks: " + repo.Tasks.Count + ", people: " + board.Users.Count);
        Console.WriteLine("-- moves");
        Move(102, Column.InProgress, "sofia");      // In progress is full already
        Move(105, Column.Done, "sofia");
        Move(101, Column.Review, "ivan");
        Move(102, Column.InProgress, "Sofia ");
        Move(103, Column.Todo, "olena");
        Console.WriteLine("-- rules (moves so far: " + TaskBoard.MovesToday + ")");
        Move(106, Column.InProgress, "taras");      // blocked
        Move(110, Column.Done, "olena");            // straight from backlog
        Move(108, Column.Done, "taras");            // own task
        Move(108, Column.Done, "Taras");
        Move(107, Column.Done, "ivan");             // 5 sp, no review
        Console.WriteLine("-- invalid input");
        Move(110, Column.Todo, "petro");            // left the company
        Move(999, Column.Todo, "olena");
        try { Move(110, Column.Todo, null); }
        catch (Exception ex) { Console.WriteLine("  crash   #110: " + ex.GetType().Name); }
        Console.WriteLine("-- team");
        Console.WriteLine("  add nadia: " + board.AddUser("Nadia", "Nadiia Bondar", "designer")
            + ", role " + board.FindUser("nadia").Role);
        int[] ids = { 110, 103, 103, 404 };
        string[] people = { "nadia", "taras", "petro", "ivan" };
        for (int i = 0; i < ids.Length; i++)
            Console.WriteLine("  assign #" + ids[i] + " to " + people[i] + ": "
                + (board.Assign(ids[i], people[i]) ? "ok" : board.LastError));
        TaskCard t = board.CreateTask("Two-factor login", 8, 7, "Feature,Auth", "olena");
        Console.WriteLine("  created " + t.Key + " in " + t.Column + ", prio " + t.Priority);
        board.Block(104, "build server disk is full");
        board.Block(106, "");
        Move(106, Column.InProgress, "ivan");       // unblocked, but no room
        Reports();
    }

    static void Move(int id, Column to, string who)
    {
        if (board.Move(id, to, who, false))
            Console.WriteLine("  ok      #" + id + " -> " + to + " by " + who.Trim());
        else
            Console.WriteLine("  refused #" + id + " -> " + to + ": " + board.LastError);
    }

    static void Reports()
    {
        Console.WriteLine("-- search");
        string[] queries = { "#ui", "@Taras", "crash", "" };
        foreach (string q in queries)
        {
            Console.WriteLine("  '" + q + "':");
            foreach (TaskCard c in board.Search(q))
            {
                string line = "    " + c.Key + " " + c.Title;
                if (c.Assignee != null)
                    line += " @" + c.Assignee;
                if (c.Points > 0)
                    line += " (" + c.Points + " sp)";
                if (c.Blocked)
                    line += " [blocked]";
                Console.WriteLine(line);
            }
        }
        Console.WriteLine("-- board");
        board.PrintBoard();
        Console.WriteLine("-- stats");
        board.PrintStats();
        Console.WriteLine("-- standup");
        new SprintReport(repo).PrintStandup();
        Console.WriteLine("-- saved " + board.Save() + " task(s), notifications: "
            + board.Sent.Count);
    }
}
