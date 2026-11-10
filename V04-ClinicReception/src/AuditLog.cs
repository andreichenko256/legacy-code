namespace ClinicReception;

// Журнал дій рецепції (у temp - на ПК немає прав на C:\Clinic)
public class AuditLog
{
    public static string FileName = Path.Combine(Path.GetTempPath(), "clinic_audit.log");

    public static void Write(string message)
    {
        try
        {
            string stamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            File.AppendAllText(FileName, stamp + " " + message + "\n");
        }
        catch
        {
        }
    }
}
