using System.Text;

namespace LibraryDesk;

// Звіти для завідувачки абонементу
public class ReportBuilder
{
    private readonly LibraryStore store;

    public ReportBuilder(LibraryStore store)
    {
        this.store = store;
    }

    // Картка читача: видачі, борг і поточна пеня
    public string ReaderSummary(string card)
    {
        Reader r = store.FindReader(card);
        if (r == null)
            return "Reader " + card + " not found";
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("Reader " + r.CardNo + " " + r.Name + " (" + r.Category + ")"
            + (r.Blocked ? " BLOCKED" : ""));
        decimal fine = 0;
        foreach (Loan l in store.Loans)
        {
            if (l.ReaderCard != r.CardNo)
                continue;
            string line = "  #" + l.Id + " " + l.InventoryNo;
            Book b = store.FindBook(l.InventoryNo);
            if (b != null)
                line = line + " '" + b.Title + "'";
            line = line + " [" + l.Status + "]";
            if (l.Extensions > 0)
                line = line + " ext x" + l.Extensions;
            sb.AppendLine(line);
            if (l.Status == LoanStatus.Returned)
                continue;
            int d = (DateTime.Today - l.DueDate.Date).Days;
            if (d > 3)
            {
                decimal p = (d - 3) * 2.5m;
                if (p > 150)
                    p = 150;
                fine = fine + p;
            }
        }
        sb.Append("  debt " + r.Debt.ToString("0.00") + ", fine " + fine.ToString("0.00"));
        return sb.ToString();
    }
}
