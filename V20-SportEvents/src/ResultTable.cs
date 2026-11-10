namespace SportEvents;

// Турнірна таблиця: очки, різниця м'ячів, перемоги.
// Повна рівність - спільне місце, за алфавітом (п. 7.3).
public class ResultTable
{
    public static int RankCalls = 0;
    public List<string> Notes = new List<string>();   // для протоколу комітету
    private TournamentRepository repo;

    public ResultTable() : this(new TournamentRepository()) { }

    public ResultTable(TournamentRepository repository)
    {
        repo = repository;
    }

    // winPoints: 3 з сезону 2019/20, архівні таблиці - по 2.
    // DNF - після всіх без місця; DQ - в самому низу.
    // TODO: п. 7.3 - особисті зустрічі замість перемог
    public List<TableRow> Rank(List<Participant> teams, int winPoints, int round,
        bool showDisqualified)
    {
        RankCalls++;
        Notes.Clear();
        var rows = new List<TableRow>();
        var active = new List<Participant>();
        var dnf = new List<Participant>();
        var dq = new List<Participant>();
        foreach (var p in teams)
        {
            p.Points = p.Wins * winPoints + p.Draws - p.PenaltyPoints;
            if (p.Status == "DQ")
            {
                Console.WriteLine("  (" + p.Name + " is disqualified, no place)");
                dq.Add(p);
            }
            else if (p.Status == "DNF")
            {
                if (DateTime.Today < new DateTime(2024, 8, 1))
                {
                    // до сезону 2024/25 очки анулювались
                    if (p.Points != 0)
                        Notes.Add(p.Name + ": " + p.Points + " pts annulled");
                    p.Points = 0;
                }
                Notes.Add(p.Name + " withdrew");
                dnf.Add(p);
            }
            else
                active.Add(p);
        }
        // бульбашка: List.Sort міняв місцями рівні команди
        for (int i = 0; i < active.Count - 1; i++)
        {
            for (int j = 0; j < active.Count - 1 - i; j++)
            {
                Participant a = active[j];
                Participant b = active[j + 1];
                if (b.Points > a.Points || (b.Points == a.Points
                    && (b.ScoredFor - b.ScoredAgainst > a.ScoredFor - a.ScoredAgainst
                    || (b.ScoredFor - b.ScoredAgainst == a.ScoredFor - a.ScoredAgainst
                    && b.Wins > a.Wins))))
                {
                    active[j] = b;
                    active[j + 1] = a;
                }
            }
        }
        //active.Sort((x, y) => x.Points != y.Points ? y.Points - x.Points
        //    : (y.ScoredFor - y.ScoredAgainst) - (x.ScoredFor - x.ScoredAgainst));
        int place = 0;
        for (int i = 0; i < active.Count; i++)
        {
            Participant p = active[i];
            Participant up = active[Math.Max(0, i - 1)];
            // нове місце, якщо команда вище строго краща
            if (i == 0 || up.Points > p.Points || (up.Points == p.Points
                && (up.ScoredFor - up.ScoredAgainst > p.ScoredFor - p.ScoredAgainst
                || (up.ScoredFor - up.ScoredAgainst == p.ScoredFor - p.ScoredAgainst
                && up.Wins > p.Wins))))
            {
                place++;
            }
            var row = new TableRow();
            row.Name = p.Name;
            row.Wins = p.Wins;
            row.Draws = p.Draws;
            row.Losses = p.Losses;
            row.ScoredFor = p.ScoredFor;
            row.ScoredAgainst = p.ScoredAgainst;
            row.Points = p.Points;
            row.Place = place;
            row.Label = place.ToString();
            rows.Add(row);
        }
        AppendOut(rows, dnf, "DNF");
        if (showDisqualified)
            AppendOut(rows, dq, "DQ");
        EventLog.Write("table", "round " + round + ": " + rows.Count + " rows");
        return rows;
    }

    // старий виклик з першої версії сайту
    public List<TableRow> Rank(List<Participant> teams)
    {
        return Rank(teams, 3, 0, true);
    }

    private void AppendOut(List<TableRow> rows, List<Participant> list, string label)
    {
        foreach (var p in list)
        {
            var row = new TableRow();
            row.Name = p.Name;
            row.Wins = p.Wins;
            row.Draws = p.Draws;
            row.Losses = p.Losses;
            row.ScoredFor = p.ScoredFor;
            row.ScoredAgainst = p.ScoredAgainst;
            row.Points = p.Points;
            row.Place = 0;
            row.Label = label;
            rows.Add(row);
        }
    }

    // для сайту: поки матч туру ще йде, таблицю не даємо
    public List<TableRow> RankDivision(string division, int round)
    {
        foreach (Match m in repo.Matches)
            if (m.Round == round && m.Status == MatchStatus.InProgress)
                return null;
        List<Participant> list = repo.ByDivision(division, true);
        if (list.Count == 0)
        {
            Console.WriteLine("No teams in division " + division);
            return null;
        }
        return Rank(list, 3, round, true);
    }
}

// Рядок таблиці для друку.
public class TableRow
{
    public int Place;
    public string Label;             // місце числом або "DNF" / "DQ"
    public string Name;
    public int Wins, Draws, Losses;
    public int ScoredFor, ScoredAgainst;
    public int Points;
}
