namespace LibraryDesk;

// Журнал операцій (файл у temp + пам'ять)
public static class AuditLog
{
    public static string FilePath = Path.Combine(Path.GetTempPath(), "librarydesk-audit.log");
    public static List<string> Recent = new List<string>();

    public static void Write(string text)
    {
        string line = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " " + text;
        Recent.Add(line);
        if (Recent.Count > 500)
            Recent.RemoveAt(0);
        try
        {
            File.AppendAllText(FilePath, line + "\n");
        }
        catch
        {
            // log is not critical, ignore
        }
    }
}
