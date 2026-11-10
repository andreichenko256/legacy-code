namespace SportEvents;

public enum MatchStatus
{
    Scheduled,
    InProgress,
    Finished,
    Cancelled
}

// Матч туру. Рахунок вносять з паперового протоколу.
public class Match
{
    public string Code;              // "M001", видає TournamentRepository
    public int Round;
    public int HomeId;
    public int AwayId;
    public int HomeGoals = -1;       // -1 = рахунок ще не внесено
    public int AwayGoals = -1;
    public MatchStatus Status = MatchStatus.Scheduled;
    public DateTime KickOff;
    public DateTime StatusChangedAt;
    public string Hall;

    public bool HasScore()
    {
        return HomeGoals >= 0 && AwayGoals >= 0;
    }
}
