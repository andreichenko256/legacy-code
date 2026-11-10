namespace RepairService;

// SMS клієнтам (TurboSMS), копія - у файл
public class Notifier
{
    public void SendSms(string phone, string text)
    {
        if (phone == null || phone.Length < 10)
        {
            Console.WriteLine("  sms skipped: bad phone");
            return;
        }
        if (text.Length > 70) // кирилиця - 70 символів на sms
            text = text.Substring(0, 67) + "...";
        Console.WriteLine("  [sms " + phone + "] " + text);
        try
        {
            string f = Path.Combine(Path.GetTempPath(), "repair_sms.log");
            File.AppendAllText(f, DateTime.Now.ToString("s") + ";" + phone + ";" + text + "\n");
        }
        catch (Exception)
        {
        }
        // старий шлюз, вимкнули у 2022
        // new System.Net.WebClient().DownloadString("http://10.0.0.15/sms?to=" + phone);
    }
}

public static class AuditLog
{
    public static string FileName = Path.Combine(Path.GetTempPath(), "repair_audit.log");

    public static void Write(string line)
    {
        try { File.AppendAllText(FileName, DateTime.Now.ToString("s") + " " + line + "\n"); }
        catch { }
    }
}
