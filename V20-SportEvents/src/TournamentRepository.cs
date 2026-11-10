namespace SportEvents;

// Сховище ліги в пам'яті, номери матчів наскрізні.
public class TournamentRepository
{
    public static int nextMatchNo = 1;
    public List<Participant> Teams = new List<Participant>();
    public List<Match> Matches = new List<Match>();

    public Participant FindTeam(int id)
    {
        foreach (Participant p in Teams)
            if (p.Id == id)
                return p;
        return null;
    }

    public Match FindMatch(string code)
    {
        foreach (Match m in Matches)
            if (m.Code == code)
                return m;
        return null;
    }

    public string NewMatchCode()
    {
        string code = "M" + nextMatchNo.ToString("000");
        nextMatchNo++;
        return code;
    }

    // withOut = true - разом зі знятими й дискваліфікованими
    public List<Participant> ByDivision(string division, bool withOut)
    {
        List<Participant> res = new List<Participant>();
        foreach (Participant p in Teams)
        {
            if (p.Division != division)
                continue;
            if (!withOut && (p.Status == "DNF" || p.Status == "DQ"))
                continue;
            res.Add(p);
        }
        return res;
    }

    // Перенесення зі старої таблиці (Excel) посеред сезону.
    // Назва;Місто;Ліга;В;Н;П;Забито;Пропущено[;Статус]
    public int ImportTeams(string[] lines)
    {
        int cnt = 0;
        foreach (string ln in lines)
        {
            if (ln == null || ln.Trim() == "" || ln.StartsWith("#"))
                continue;
            try
            {
                string[] f = ln.Split(';');
                string div = f[2].Trim().ToUpper();
                if (div != "A" && div != "B")
                    div = "B";   // тимчасово: в Excel ще є стара ліга "C"
                Participant p = new Participant(Teams.Count + 1, f[0].Trim(), f[1].Trim(), div);
                Teams.Add(p);
                p.Wins = int.Parse(f[3]);
                p.Draws = int.Parse(f[4]);
                p.Losses = int.Parse(f[5]);
                p.ScoredFor = int.Parse(f[6]);
                p.ScoredAgainst = int.Parse(f[7]);
                if (f.Length > 8 && f[8].Trim() != "")
                    p.Status = f[8].Trim().ToUpper();
                cnt++;
            }
            catch (Exception)   // кривий рядок пропускаємо
            {
            }
        }
        return cnt;
    }

    public void PrintDivision(string division)
    {
        Console.WriteLine("Division " + division + " in registration order:");
        foreach (Participant r in Teams)
        {
            if (r.Division != division)
                continue;
            Console.Write(("#" + r.Id).PadRight(4));
            string line = r.Name.PadRight(20);
            line += r.Wins.ToString().PadLeft(4);
            line += r.Draws.ToString().PadLeft(4);
            line += r.Losses.ToString().PadLeft(4);
            line += (r.ScoredFor + ":" + r.ScoredAgainst).PadLeft(8);
            line += r.Points.ToString().PadLeft(5);
            Console.WriteLine(line);
        }
    }
}
