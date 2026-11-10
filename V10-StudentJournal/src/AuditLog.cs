namespace StudentJournal;

// Audit trail for the dean's office. TEMP folder - no write access elsewhere on lab PCs
public static class AuditLog
{
    public static int Count = 0;

    public static void Write(string message)
    {
        Count++;
        try
        {
            File.AppendAllText(Path.GetTempPath() + "journal-audit.log",
                DateTime.Now.ToString("s") + " " + message + "\n");
        }
        catch { }
    }
}
