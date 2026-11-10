namespace FleetMaintenance;

// Журнал дій. Читає тільки адмін, на екран не йде
public class AuditLog
{
    public static int Written = 0;
    public static string FileName = Path.Combine(Path.GetTempPath(), "fleet-audit.log");

    public static void Write(string text)
    {
        try
        {
            File.AppendAllText(FileName, DateTime.Now.ToString("s") + " " + text + "\n");
            Written++;
        }
        catch { }
    }
}
