namespace LibraryDesk;

// Видача та продовження книг.
// Пеню рахуємо тут же, без ReturnDesk.
public class LoanManager
{
    // не більше 2 продовжень (правила, п. 4.3)
    private const int MAX_EXT = 2;
    private static bool traceExtend = false;

    private LibraryStore store;
    public string LastError = "";
    public decimal LastFine;
    public int LastTerm;
    public int IssuedToday;
    public int DeniedToday;
    public string DeskName = "DESK-1";
    // заповнюються в Issue
    private Reader curReader;
    private Book curBook;

    public LoanManager() : this(new LibraryStore())
    {
    }

    public LoanManager(LibraryStore store)
    {
        this.store = store;
    }

    // Видача книги читачу.
    // Повертає номер видачі або -1 (див. LastError).
    // readingRoom - видача в читальну залу
    public int Issue(string cardNo, string invNo, int days, bool readingRoom)
    {
        LastError = "";
        LastFine = 0;
        // рахуємо відмову наперед, при видачі віднімаємо
        DeniedToday++;
        if (string.IsNullOrWhiteSpace(cardNo))
        {
            LastError = "Card number is empty";
            return -1;
        }
        curReader = store.FindReader(cardNo.Trim().ToUpper());
        if (curReader == null)
        {
            LastError = "Reader not found: " + cardNo;
            return -1;
        }
        curBook = store.FindBook(invNo);
        if (curBook == null || !curBook.Available)
        {
            LastError = "Book is not available: " + invNo;
            return -1;
        }

        // пеня по всіх неповернених книгах читача
        decimal fine = 0;
        int onHand = 0;
        foreach (Loan l in store.Loans)
        {
            if (l.ReaderCard == curReader.CardNo && l.Status != LoanStatus.Returned)
            {
                onHand++;
                int overdue = (DateTime.Today - l.DueDate.Date).Days;
                if (overdue > 3)
                {
                    decimal pen = (overdue - 3) * 2.5m;
                    if (pen > 150)
                        pen = 150;
                    fine = fine + pen;
                }
            }
        }
        LastFine = fine;
        if (fine + curReader.Debt > 50)
        {
            LastError = "Debt limit exceeded: " + (fine + curReader.Debt).ToString("0.00");
            AuditLog.Write(DeskName + " DENY " + curReader.CardNo + " debt "
                + (fine + curReader.Debt));
            return -1;
        }
        if (onHand >= LoanLimit(curReader))
        {
            LastError = "Loan limit reached: " + onHand + " of " + LoanLimit(curReader);
            return -1;
        }

        // термін видачі, днів
        int term = 14;
        if (curReader.Category == "student")
            term = 30;
        if (curBook.Kind == "periodical")
            term = 7;
        // якщо книга довідкова або рідкісна,
        // то тільки в зал і повернути в той же день
        if (curBook.Kind == "reference" || curBook.Rare)
        {
            if (!readingRoom)
            {
                LastError = "Book " + curBook.InventoryNo + " is for the reading room only";
                return -1;
            }
            if (DateTime.Now.Hour >= 19)
            {
                LastError = "Reading room closes at 20:00, no issues after 19:00";
                return -1;
            }
            term = 0;
        }
        else if (fine > 0)
        {
            // боржникам - тиждень, поки не сплатять
            term = 7;
        }
        if (days > 0 && days < term)
            term = days;

        Loan loan = new Loan { Id = LibraryStore.NextLoanId++,
            ReaderCard = curReader.CardNo, InventoryNo = curBook.InventoryNo,
            IssuedOn = DateTime.Today, DueDate = DateTime.Today.AddDays(term) };
        store.Loans.Add(loan);
        curBook.Available = false;
        LastTerm = term;
        IssuedToday++;
        DeniedToday--;
        AuditLog.Write(DeskName + " ISSUE #" + loan.Id + " " + loan.ReaderCard + " "
            + loan.InventoryNo + " " + term + "d");
        return loan.Id;
    }

    // Продовження видачі (за замовчуванням на 14 днів)
    public bool Extend(int loanId, int days)
    {
        Loan loan = store.FindLoan(loanId);
        if (loan == null)
        {
            LastError = "Loan not found: " + loanId;
            return false;
        }
        if (loan.Status == LoanStatus.Returned || loan.Status == LoanStatus.Lost)
        {
            LastError = "Loan #" + loanId + " is closed (" + loan.Status + ")";
            return false;
        }
        if (loan.Extensions > MAX_EXT)
        {
            LastError = "Extension limit reached for loan #" + loanId;
            return false;
        }
        if (IsOverdue(loan))
        {
            LastError = "Loan #" + loanId + " is overdue, pay the fine first";
            return false;
        }
        Book b = store.FindBook(loan.InventoryNo);
        if (b != null && (b.Kind == "periodical" || b.Rare))
        {
            LastError = "Item " + loan.InventoryNo + " cannot be extended";
            return false;
        }
        // TODO: check the reservation queue first
        if (days <= 0 || days > 30)
            days = 14;
        loan.DueDate = loan.DueDate.AddDays(days);
        loan.Extensions++;
        loan.Status = LoanStatus.Extended;
        if (traceExtend)
            Console.WriteLine("extend #" + loanId + " -> " + loan.DueDate.ToShortDateString());
        AuditLog.Write(DeskName + " EXTEND #" + loanId + " +" + days);
        return true;
    }

    // прострочена, якщо зараз пізніше дати повернення
    public bool IsOverdue(Loan loan)
    {
        if (loan.Status == LoanStatus.Returned)
            return false;
        return DateTime.Now > loan.DueDate;
    }

    // max books on hand, by reader category
    public int LoanLimit(Reader r)
    {
        // тимчасово: заблокованим 0, Issue сам відмовить
        if (r.Blocked)
            return 0;
        switch (r.Category)
        {
            case "staff":
                return 10;
            case "child":
                return 3;
            default:
                return 5;
        }
    }
}
