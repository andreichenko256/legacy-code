namespace PharmacyStock;

// Журнал операцій для перевірок Держлікслужби
public static class AuditLog
{
    public static bool Enabled = true;
    public static string FilePath = Path.Combine(Path.GetTempPath(), "pharmacy-audit.log");

    public static void Write(string op, string text)
    {
        if (!Enabled)
            return;
        try
        {
            string line = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + ";" + op + ";" + text;
            File.AppendAllText(FilePath, line + "\n");
        }
        catch { }
    }
}
