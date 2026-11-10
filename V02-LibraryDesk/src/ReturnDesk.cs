namespace LibraryDesk;

// Стійка повернення: книги і втрати
public class ReturnDesk
{
    private LibraryStore store;

    public ReturnDesk(LibraryStore store)
    {
        this.store = store;
    }

    // Повернення книги: пеня або -1, якщо нема
    // Пеня: 2 грн за день після 5 пільгових днів
    public decimal Return(int loanId)
    {
        Loan loan = store.FindLoan(loanId);
        if (loan == null)
        {
            Console.WriteLine("  [desk] loan #" + loanId + " not found");
            return -1;
        }
        if (loan.Status == LoanStatus.Returned)
            throw new InvalidOperationException("Loan #" + loanId + " is already returned");

        decimal charged = 0;
        // за втрачену вже виставили рахунок
        if (loan.Status != LoanStatus.Lost)
        {
            int daysLate = (DateTime.Today - loan.DueDate.Date).Days;
            if (daysLate > 3)
            {
                decimal fineAmt = (daysLate - 3) * 2.5m;
                if (fineAmt > 150)
                    fineAmt = 150;
                charged = charged + fineAmt;
            }
        }
        //int days = (DateTime.Today - loan.IssuedOn).Days - 30;
        //if (days > 0)
        //    charged = days * 2;

        Reader r = store.FindReader(loan.ReaderCard);
        if (r != null)
        {
            r.Debt = r.Debt + charged;
            if (r.Debt > 50)
                r.Blocked = true;
        }
        loan.Status = LoanStatus.Returned;
        loan.ReturnedOn = DateTime.Today;
        Book b = store.FindBook(loan.InventoryNo);
        if (b != null)
            b.Available = true;
        Console.WriteLine("  [desk] #" + loan.Id + " returned, fine " + charged.ToString("0.00"));
        AuditLog.Write("RETURN #" + loan.Id + " fine " + charged);
        return charged;
    }

    // Книгу втрачено: рахунок читачу
    public void MarkLost(int loanId)
    {
        Loan loan = store.FindLoan(loanId);
        if (loan == null || loan.Status == LoanStatus.Returned
            || loan.Status == LoanStatus.Lost)
            return;
        decimal cost = calcReplacementCost(store.FindBook(loan.InventoryNo));
        Reader r = store.FindReader(loan.ReaderCard);
        if (r != null)
        {
            r.Debt = r.Debt + cost;
            if (r.Debt > 50)
                r.Blocked = true;
        }
        loan.Status = LoanStatus.Lost;
        Console.WriteLine("  [desk] #" + loan.Id + " lost, charged " + cost.ToString("0.00"));
        AuditLog.Write("LOST #" + loan.Id + " " + cost);
    }

    // how much the reader pays for a lost book
    private decimal calcReplacementCost(Book b)
    {
        if (b == null)
            return 200; // ціна невідома - мінімум
        if (b.Rare)
            return b.Price * 10;
        if (b.Kind == "periodical")
            return b.Price;
        return b.Price * 3;
    }
}
