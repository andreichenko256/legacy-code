namespace LibraryDesk;

// Сховище в пам'яті. TODO: перенести на БД
public class LibraryStore
{
    // спільний лічильник номерів видач
    public static int NextLoanId = 1001;

    public List<Reader> Readers = new List<Reader>();
    public List<Book> Books = new List<Book>();
    public List<Loan> Loans = new List<Loan>();

    public Reader FindReader(string card)
    {
        if (card == null)
            return null;
        string key = card.Trim().ToUpper();
        foreach (Reader r in Readers)
        {
            if (r.CardNo == key)
                return r;
        }
        return null;
    }

    public Book FindBook(string invNo)
    {
        if (invNo == null)
            return null;
        foreach (Book b in Books)
        {
            if (b.InventoryNo == invNo)
                return b;
        }
        return null;
    }

    public Loan FindLoan(int id)
    {
        if (id <= 0)
            return null;
        for (int i = 0; i < Loans.Count; i++)
        {
            if (Loans[i].Id == id)
                return Loans[i];
        }
        return null;
    }

    public void AddReader(string card, string name, string phone, string category,
        decimal debt, bool blocked)
    {
        Readers.Add(new Reader { CardNo = card.ToUpper(), Name = name, Phone = phone,
            Category = category, Debt = debt, Blocked = blocked });
    }

    public void AddBook(string invNo, string title, string author, string kind, bool rare,
        decimal price)
    {
        Books.Add(new Book { InventoryNo = invNo, Title = title, Author = author,
            Kind = kind, Rare = rare, Price = price });
    }
}
