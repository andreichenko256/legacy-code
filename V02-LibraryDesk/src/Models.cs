namespace LibraryDesk;

// Статуси видачі. Lost додали пізніше
public enum LoanStatus
{
    Issued,
    Extended,
    Returned,
    Lost
}

public class Reader
{
    public string CardNo { get; set; }
    public string Name { get; set; }
    public string Phone;
    // adult, student, staff, child, pensioner
    public string Category = "adult";
    public decimal Debt;
    public bool Blocked;
}

public class Book
{
    public string InventoryNo;
    public string Title;
    public string Author { get; set; }
    // fiction / textbook / periodical / reference
    public string Kind = "fiction";
    public bool Rare;
    public decimal Price { get; set; }
    public bool Available = true;
}

// Видача (формуляр): одна книга на руках
public class Loan
{
    public int Id;
    public string ReaderCard;
    public string InventoryNo;
    public DateTime IssuedOn { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime? ReturnedOn;
    public int Extensions;
    public LoanStatus Status = LoanStatus.Issued;
}
