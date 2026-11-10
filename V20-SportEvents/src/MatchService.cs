namespace SportEvents;

// Календар і протоколи матчів.
// Очки тут не рахуються, лише В/Н/П і м'ячі (див. ResultTable).
public class MatchService
{
    public string LastError = "";
    public int ResultsEntered = 0;
    private TournamentRepository repo;

    public MatchService(TournamentRepository repository)
    {
        repo = repository;
    }

    // days - зсув від сьогодні (може бути < 0), hour - година
    public string Schedule(int round, int homeId, int awayId, string hall, int days, int hour)
    {
        LastError = "";
        Participant h = repo.FindTeam(homeId);
        Participant a = repo.FindTeam(awayId);
        if (h == null || a == null)
            LastError = "Team not found";
        else if (homeId == awayId || h.Division != a.Division)
            LastError = "Bad pair";
        else if (h.Status == "DQ" || a.Status == "DQ")
            LastError = "Disqualified team cannot play";
        if (LastError != "")
            return null;
        Match m = new Match();
        m.Code = repo.NewMatchCode();
        m.Round = round;
        m.HomeId = homeId;
        m.AwayId = awayId;
        m.Hall = hall;
        m.KickOff = DateTime.Today.AddDays(days).AddHours(hour);
        m.StatusChangedAt = DateTime.Now;
        repo.Matches.Add(m);
        EventLog.Write("schedule", m.Code + " " + h.Name + " - " + a.Name);
        return m.Code;
    }

    public bool ChangeStatus(string code, MatchStatus newStatus)
    {
        Match m = repo.FindMatch(code);
        if (m == null)
        {
            Console.WriteLine("  Match " + code + " not found");
            return false;
        }
        bool ok = false;
        switch (m.Status)
        {
            case MatchStatus.Scheduled:
                ok = newStatus == MatchStatus.InProgress || newStatus == MatchStatus.Cancelled;
                break;
            case MatchStatus.InProgress:
                // без рахунку в протоколі матч не закриваємо
                ok = (newStatus == MatchStatus.Finished && m.HasScore())
                    || newStatus == MatchStatus.Cancelled;
                break;
            case MatchStatus.Finished:
            case MatchStatus.Cancelled:
                ok = false;   // протокол підписано або матч знято
                break;
        }
        if (!ok)
        {
            Console.WriteLine("  Cannot change " + code + ": " + m.Status + " -> " + newStatus);
            return false;
        }
        m.Status = newStatus;
        m.StatusChangedAt = DateTime.Now;
        if (newStatus == MatchStatus.Finished)
            ApplyResult(m);
        EventLog.Write("status", code + " " + newStatus);
        return true;
    }

    // рахунок можна вносити, поки матч не закрито
    public bool EnterResult(string code, int home, int away)
    {
        LastError = "";
        Match m = repo.FindMatch(code);
        if (m == null)
            LastError = "Match " + code + " not found";
        else if (m.Status == MatchStatus.Cancelled || m.Status == MatchStatus.Finished)
            LastError = "Match " + code + " is " + m.Status;
        else if (home < 0 || away < 0 || home + away > 30)   // typo in the sheet
            LastError = "Bad score " + home + ":" + away;
        else if (m.KickOff > DateTime.Now)
            LastError = "Match " + code + " has not started yet";
        if (LastError != "")
            return false;
        m.HomeGoals = home;
        m.AwayGoals = away;
        ResultsEntered++;
        return true;
    }

    private void ApplyResult(Match m)
    {
        Participant home = repo.FindTeam(m.HomeId);
        Participant away = repo.FindTeam(m.AwayId);
        home.ScoredFor += m.HomeGoals;
        home.ScoredAgainst += m.AwayGoals;
        away.ScoredFor += m.AwayGoals;
        away.ScoredAgainst += m.HomeGoals;
        if (m.HomeGoals > m.AwayGoals)
            home.Wins++;
        else if (m.HomeGoals < m.AwayGoals)
            away.Wins++;
        else
        {
            home.Draws++;
            away.Draws++;
        }
        // поразки - окремо, колонку додали у 2021
        if (m.HomeGoals < m.AwayGoals)
            home.Losses++;
        if (m.HomeGoals > m.AwayGoals)
            away.Losses++;
    }
}
