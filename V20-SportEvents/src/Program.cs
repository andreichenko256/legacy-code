namespace SportEvents;

// Прогін 10-го туру міської футзальної ліги
public class Program
{
    // стан після 9-го туру, формат - див. ImportTeams
    private static string[] teamLines =
    {
        "Kyiv Falcons;Kyiv;A;6;2;1;30;12", "Odesa Mariners;Odesa;A;4;4;1;24;14",
        "Lviv Lions;Lviv;A;5;3;1;27;15", "Dnipro Steel;Dnipro;A;5;2;2;22;18",
        "Kharkiv Tigers;Kharkiv;a;4;5;0;18;14", "Poltava Bears;Poltava;A;1;2;5;9;21;DNF",
        "Vinnytsia Rapid;Vinnytsia;A;2;1;6;11;26;DQ", "Sumy Wolves;Sumy;A;3;2;x;10;12",
        "", "# division B", "Chernihiv Eagles;Chernihiv;B;4;1;1;18;9",
        "Zhytomyr Union;Zhytomyr;C;3;2;1;14;10"
    };

    // протокол дня: код матчу і новий статус або рахунок
    private static string[] matchDay =
    {
        "M001 InProgress", "M001 1:1", "M001 Finished", "M002 2:0", "M002 Finished",
        "M002 InProgress", "M002 Finished", "M003 InProgress", "M003 -1:2", "M003 2:2",
        "M003 Finished", "M004 Cancelled", "M004 3:0", "M004 InProgress", "M005 1:0",
        "M999 1:0", "M999 InProgress", "M001 Cancelled"
    };

    public static void Main(string[] args)
    {
        var repo = new TournamentRepository();
        var service = new MatchService(repo);
        var table = new ResultTable(repo);
        var printer = new ReportPrinter();
        Console.WriteLine("=== Import ===");
        int n = repo.ImportTeams(teamLines);
        Console.WriteLine("Imported " + n + " line(s), teams in list: " + repo.Teams.Count);
        Console.WriteLine("=== Fixtures ===");
        Fixtures(service);
        Console.WriteLine("=== Match day ===");
        foreach (string cmd in matchDay)
            Play(service, cmd);
        repo.FindTeam(8).PenaltyPoints = 3;   // рішення комітету
        Console.WriteLine("=== Tables ===");
        List<TableRow> rows = table.Rank(repo.ByDivision("A", true), 3, 10, true);
        printer.PrintTable("Division A after round 10", rows);
        Console.WriteLine("Notes: " + string.Join("; ", table.Notes));
        printer.print_podium(rows);
        rows = table.Rank(repo.ByDivision("A", true), 2, 10, false);
        printer.PrintTable("Division A, archive scoring", rows);
        printer.PrintTable("Division B after round 10", table.RankDivision("B", 10));
        repo.PrintDivision("B");
        if (table.RankDivision("C", 10) == null)
            Console.WriteLine("Division C: no table");
        Console.WriteLine("Empty list: " + table.Rank(new List<Participant>(), 3, 1, true).Count);
        try
        {
            table.Rank(null, 3, 10, true);
        }
        catch (Exception e)
        {
            Console.WriteLine("Rank(null): " + e.GetType().Name);
        }
        Console.WriteLine("Rank calls: " + ResultTable.RankCalls);
    }

    private static void Fixtures(MatchService service)
    {
        // тур, господарі, гості, днів від сьогодні
        int[,] plan = { { 10, 1, 3, -1 }, { 10, 2, 8, -1 }, { 10, 4, 5, -1 }, { 10, 6, 7, -1 },
            { 11, 5, 6, 2 }, { 11, 3, 2, 3 }, { 12, 1, 1, 7 }, { 12, 1, 9, 7 }, { 12, 1, 42, 7 } };
        for (int i = 0; i < plan.GetLength(0); i++)
        {
            int home = plan[i, 1];
            int away = plan[i, 2];
            string code = service.Schedule(plan[i, 0], home, away, "Hall 1", plan[i, 3], 18);
            if (code == null)
                Console.WriteLine("Rejected #" + home + " vs #" + away + ": " + service.LastError);
            else
                Console.WriteLine("Scheduled " + code + ": #" + home + " vs #" + away);
        }
    }

    private static void Play(MatchService service, string cmd)
    {
        string[] p = cmd.Split(' ');
        if (p[1].Contains(':'))
        {
            string[] g = p[1].Split(':');
            if (service.EnterResult(p[0], int.Parse(g[0]), int.Parse(g[1])))
                Console.WriteLine("  " + p[0] + " score " + p[1]);
            else
                Console.WriteLine("  " + p[0] + " score rejected: " + service.LastError);
        }
        else if (service.ChangeStatus(p[0], Enum.Parse<MatchStatus>(p[1])))
            Console.WriteLine("  " + p[0] + " -> " + p[1]);
    }
}
