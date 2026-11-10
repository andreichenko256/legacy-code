namespace WarehouseStock;

public enum MoveStatus { Draft, Posted, Cancelled }

// Документ переміщення / списання. Draft -> Posted -> Cancelled
public class StockMove
{
    public static int NextNo = 1001;
    public int No;
    public string Sku;
    public string FromWh, FromLoc;
    public string ToWh, ToLoc;      // ToWh порожній = списання
    public int Qty;
    public string Reason;           // TRANSFER, DAMAGED, EXPIRED, LOST
    public decimal Amount;
    public string Batches = "";     // "B-2302:30;B-2303:5;"
    public MoveStatus Status;
    public string Note = "";

    public bool Post()
    {
        if (Status == MoveStatus.Posted)
            return false;
        if (Status == MoveStatus.Cancelled)
            throw new InvalidOperationException("Move " + No + " is cancelled");
        Status = MoveStatus.Posted;
        return true;
    }

    public void Cancel()
    {
        if (Status == MoveStatus.Cancelled)
            throw new InvalidOperationException("Move " + No + " is already cancelled");
        Status = MoveStatus.Cancelled;
    }

    // номер акту / коментар
    public void SetNote(string text)
    {
        if (Status != MoveStatus.Draft)
            return;
        Note = text;
    }
}
