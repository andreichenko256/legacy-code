namespace SportEvents;

// Команда-учасник турніру.
// Status: "OK" - грає, "DNF" - знялася, "DQ" - дискваліфікована.
public class Participant
{
    public int Id;
    public string Name;
    public string City;
    public string Division;          // "A" - вища ліга, "B" - перша
    public string Status = "OK";
    public int Wins;
    public int Draws;
    public int Losses;
    public int ScoredFor;
    public int ScoredAgainst;
    public int PenaltyPoints;        // штрафні очки від комітету
    public int Points;               // рахує ResultTable.Rank
    public string Coach;
    public string Phone;             // як ввели в заявці
    public List<string> Remarks = new List<string>();

    public Participant(int id, string name, string city, string division)
    {
        Id = id;
        Name = name;
        City = city;
        Division = division;
    }
}
