namespace CinemaTickets;

public class Movie
{
    public int Id;
    public string Title;
    public int Minutes;
    public int AgeLimit; // 0, 12, 16, 18
}

// VipRows - скільки останніх рядів з диванами
public class Hall
{
    public int Id;
    public string Name;
    public int Rows, SeatsPerRow;
    public int VipRows;
    public bool Imax;
}

public class Session
{
    public int Id;
    public Movie Movie;
    public Hall Hall;
    public DateTime StartsAt;
    public string Format; // "2D", "3D", "IMAX"

    // movie + 15 min of ads + 10 min of cleaning
    public DateTime EndsAt()
    {
        return StartsAt.AddMinutes(Movie.Minutes + 15 + 10);
    }
}
