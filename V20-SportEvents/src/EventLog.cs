namespace SportEvents;

// Журнал дій секретаря для комітету, у тимчасовій теці.
public static class EventLog
{
    public static bool Enabled = true;

    public static void Write(string kind, string text)
    {
        if (!Enabled)
            return;
        try
        {
            string path = Path.Combine(Path.GetTempPath(), "sport-events-journal.log");
            File.AppendAllText(path, DateTime.Now.ToString("s") + " " + kind + ": " + text + "\n");
        }
        catch { }   // never stop the desk because of the log
    }
}
