namespace CinemaTickets;

public enum TicketStatus { Reserved, Paid, Used, Refunded, Expired }

public class Ticket
{
    public int Id;
    public string Number; // CT-00001
    public int SessionId;
    public int Row, Seat;
    public string Category = "adult"; // adult / student / child
    public decimal Price;
    public TicketStatus Status = TicketStatus.Reserved;
    public DateTime ReservedAt;
    public string Customer;
    public string Phone;

    // Reserved -> Paid / Expired, Paid -> Used / Refunded
    public bool ChangeStatus(TicketStatus to)
    {
        bool ok = false;
        switch (Status)
        {
            case TicketStatus.Reserved:
                ok = to == TicketStatus.Paid || to == TicketStatus.Expired;
                break;
            case TicketStatus.Paid:
                ok = to == TicketStatus.Used || to == TicketStatus.Refunded;
                break;
        }
        if (ok)
            Status = to;
        return ok;
    }
}
