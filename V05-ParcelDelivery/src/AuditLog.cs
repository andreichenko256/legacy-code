namespace ParcelDelivery;

// Журнал операцій для підтримки (тимчасова тека).
public static class AuditLog
{
    public static bool Enabled = true;

    public static void Write(string source, string text)
    {
        if (!Enabled) return;
        try
        {
            string file = Path.Combine(Path.GetTempPath(), "parcel-delivery-audit.log");
            string line = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " [" + source + "] ";
            File.AppendAllText(file, line + text + "\n");
        }
        catch { }
    }
}
