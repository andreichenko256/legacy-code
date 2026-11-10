namespace CinemaTickets;

// Everything is kept in memory. TODO: move to a database
public class CinemaRepository
{
    public static int nextTicketNo = 1;
    public List<Movie> Movies = new List<Movie>();
    public List<Hall> Halls = new List<Hall>();
    public List<Session> Sessions = new List<Session>();
    public List<Ticket> Tickets = new List<Ticket>();

    public Session FindSession(int id)
    {
        foreach (Session s in Sessions)
            if (s.Id == id)
                return s;
        return null;
    }

    // номер виду CT-00042, де 42 - Id квитка
    public Ticket FindTicket(string number)
    {
        int id = 0;
        try { id = int.Parse(number.Substring(3)); }
        catch (Exception) { }
        foreach (Ticket t in Tickets)
            if (t.Id == id)
                return t;
        return null;
    }

    public Ticket NewTicket(int sessionId, int row, int seat)
    {
        Ticket t = new Ticket();
        t.Id = nextTicketNo++;
        t.Number = "CT-" + t.Id.ToString("D5");
        t.SessionId = sessionId;
        t.Row = row;
        t.Seat = seat;
        t.ReservedAt = DateTime.Now;
        Tickets.Add(t);
        return t;
    }

    public bool isSeatTaken(int sessionId, int row, int seat)
    {
        foreach (Ticket t in Tickets)
        {
            if (t.SessionId == sessionId && t.Row == row && t.Seat == seat
                && t.Status != TicketStatus.Refunded
                && t.Status != TicketStatus.Expired)
                return true;
        }
        return false;
    }

    // returns null if the format is not supported or the hall is busy
    public Session AddSession(int id, Movie m, Hall h, DateTime start, string format)
    {
        string f = format.ToUpper();
        if (f == "2D" || f == "3D" || f == "IMAX")
        {
            if (f == "IMAX" && !h.Imax)
                return null;
            DateTime end = start.AddMinutes(m.Minutes + 25);
            foreach (Session x in Sessions)
            {
                // overlaps in time in the same hall
                if (x.Hall.Id == h.Id && start < x.EndsAt() && x.StartsAt < end)
                    return null;
            }
            Session s = new Session { Id = id, Movie = m, Hall = h, StartsAt = start };
            s.Format = format;
            Sessions.Add(s);
            return s;
        }
        return null;
    }
}
