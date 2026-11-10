namespace ClinicReception;

// Розклад. Сюди ж смс, відпустки лікарів, статистика
public class SchedulePlanner
{
    private ClinicRepository repo = ClinicRepository.Current;
    private FeeCalculator fees = new FeeCalculator();
    private string lastError = "";
    private DateTime foundAt;
    private List<string> holidays = new List<string>(); // "doctorId|from|to"
    private string clinicName = "City Clinic #3";
    public static bool SmsEnabled = true;
    private static int smsCounter = 0;
    private List<string> outbox = new List<string>();
    private int bookedCount = 0;
    private int urgentCount = 0;
    private int failedCount = 0;
    private Dictionary<string, int> perType = new Dictionary<string, int>();

    // Повертає null, причину див. у GetLastError()
    public Appointment Book(int patientId, int doctorId, string visitType, DateTime day,
        bool urgent)
    {
        lastError = "";
        Patient p = repo.FindPatient(patientId);
        Doctor d = repo.FindDoctor(doctorId);
        if (p == null || d == null)
        {
            lastError = "Patient or doctor not found";
            return null;
        }
        if (day.Date < DateTime.Today || IsOnHoliday(doctorId, day))
        {
            lastError = "Date is in the past or doctor is on holiday";
            return null;
        }
        int len;
        switch (visitType)
        {
            case "CONS": len = 20; break;
            case "FUP": len = 15; break;
            case "PROC": len = 40; break;
            case "VACC": len = 10; break;
            default: len = 20; break;
        }
        //if (day.DayOfWeek == DayOfWeek.Sunday)
        //    { lastError = "Clinic is closed on Sunday"; return null; }
        List<Appointment> dayList = GetDoctorDay(doctorId, day);
        // записи пацієнта на цей день (до всіх лікарів)
        List<Appointment> mine = repo.FindForPatient(patientId, day);
        DateTime from = day.Date.AddHours(d.StartHour);
        DateTime to = day.Date.AddHours(d.EndHour);
        if (!urgent)
            to = to.AddMinutes(-30); // остання година - для термінових
        if (day.Date == DateTime.Today && from < DateTime.Now)
            from = DateTime.Today.AddHours(DateTime.Now.Hour + 1);
        foundAt = DateTime.MinValue;
        // перебір слотів по 5 хв: вільні і лікар, і пацієнт
        for (DateTime t = from; t.AddMinutes(len) <= to; t = t.AddMinutes(5))
        {
            if (t.Hour == 13)
                continue; // обід
            bool ok = true;
            foreach (Appointment a in dayList)
            {
                if (a.Start < t.AddMinutes(len))
                {
                    if (t < a.Start.AddMinutes(a.Minutes))
                    {
                        ok = false;
                        break;
                    }
                }
            }
            foreach (Appointment a in mine)
            {
                // між візитами пацієнта мінімум 30 хв
                if (Math.Abs((a.Start - t).TotalMinutes) < 30)
                    ok = false;
            }
            if (ok)
            {
                foundAt = t;
                break;
            }
        }
        if (foundAt == DateTime.MinValue)
        {
            failedCount++;
            lastError = "No free slot for " + visitType;
            return null;
        }
        Appointment ap = new Appointment { Id = ClinicRepository.NextId(), PatientId = patientId,
            DoctorId = doctorId, VisitType = visitType, Start = foundAt, Minutes = len };
        ap.Urgent = urgent;
        ap.Status = AppointmentStatus.Booked;
        ap.Fee = fees.CalcFee(ap, p);
        repo.Appointments.Add(ap);
        count_stats(ap);
        SendSms(p.Phone, formatSms(ap, p, d));
        AuditLog.Write("BOOK #" + ap.Id + " patient " + patientId + " doctor " + doctorId);
        return ap;
    }

    public string GetLastError() { return lastError; }
    public List<string> GetOutbox() { return outbox; }
    public int GetSmsCount() { return smsCounter; }
    public int GetBookedCount() { return bookedCount; }
    public void ClearOutbox() { outbox.Clear(); }

    public List<Appointment> GetDoctorDay(int doctorId, DateTime day)
    {
        List<Appointment> dayList = new List<Appointment>();
        foreach (Appointment x in repo.Appointments)
        {
            if (x.DoctorId != doctorId || x.Start.Date != day.Date)
                continue;
            if (x.Status == AppointmentStatus.Cancelled)
                continue;
            dayList.Add(x);
        }
        return dayList;
    }

    public Appointment Reschedule(int appointmentId, DateTime newDay)
    {
        Appointment old = repo.FindAppointment(appointmentId);
        if (old == null)
            return null;
        AppointmentStatus was = old.Status;
        old.Status = AppointmentStatus.Cancelled;
        Appointment n = Book(old.PatientId, old.DoctorId, old.VisitType, newDay, old.Urgent);
        if (n == null)
            old.Status = was;
        return n;
    }

    public bool AddHoliday(int doctorId, DateTime from, DateTime to)
    {
        if (to < from)
            return false;
        holidays.Add(doctorId + "|" + from.ToString("yyyy-MM-dd") + "|"
            + to.ToString("yyyy-MM-dd"));
        AuditLog.Write("HOLIDAY doctor " + doctorId);
        return true;
    }

    public bool IsOnHoliday(int doctorId, DateTime day)
    {
        foreach (string h in holidays)
        {
            string[] parts = h.Split('|');
            try
            {
                if (int.Parse(parts[0]) == doctorId && day.Date >= DateTime.Parse(parts[1])
                    && day.Date < DateTime.Parse(parts[2]))
                    return true;
            }
            catch { }
        }
        return false;
    }

    public int CancelHolidays(int doctorId)
    {
        return holidays.RemoveAll(h => h.StartsWith(doctorId + "|"));
    }

    public string formatSms(Appointment a, Patient p, Doctor d)
    {
        string vt;
        switch (a.VisitType)
        {
            case "CONS": vt = "consultation"; break;
            case "FUP": vt = "follow-up"; break;
            case "PROC": vt = "procedure"; break;
            case "VACC": vt = "vaccination"; break;
            default: vt = "visit"; break;
        }
        return clinicName + ": " + p.FullName + ", " + vt + " with Dr. " + d.Name + " at "
            + a.Start.ToString("HH:mm") + ", room " + d.Room + (a.Urgent ? " (URGENT)" : "");
    }

    public bool SendSms(string phone, string text)
    {
        string to = normPhone(phone);
        if (!SmsEnabled || to == "")
            return false;
        smsCounter++;
        outbox.Add(to + ": " + text);
        // TODO: підключити шлюз, поки що в консоль
        Console.WriteLine("  [SMS " + smsCounter + " -> " + to + "] " + text);
        return true;
    }

    // 067 123 45 67 / (067)123-45-67 / 380671234567 -> +380671234567
    public string normPhone(string phone)
    {
        if (phone == null)
            return "";
        string s = phone.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");
        if (s.StartsWith("+"))
            return s;
        if (s.StartsWith("380"))
            return "+" + s;
        if (s.StartsWith("0") && s.Length == 10)
            return "+38" + s;
        return "";
    }

    private void count_stats(Appointment ap)
    {
        bookedCount++;
        if (ap.Urgent)
            urgentCount++;
        perType[ap.VisitType] = perType.ContainsKey(ap.VisitType) ? perType[ap.VisitType] + 1 : 1;
    }

    // звіт для старшої медсестри в кінці зміни
    public string BuildDayReport(int doctorId, DateTime day)
    {
        Doctor d = repo.FindDoctor(doctorId);
        if (d == null)
            return "";
        List<Appointment> dayList = new List<Appointment>();
        foreach (Appointment x in repo.Appointments)
        {
            if (x.DoctorId != doctorId || x.Start.Date != day.Date)
                continue;
            if (x.Status == AppointmentStatus.Cancelled)
                continue;
            dayList.Add(x);
        }
        string r = "Day report: " + DescribeDoctor(d) + "\n";
        int mins = 0;
        foreach (Appointment a in dayList)
        {
            string vt;
            switch (a.VisitType)
            {
                case "CONS": vt = "consultation"; break;
                case "FUP": vt = "follow-up"; break;
                case "PROC": vt = "procedure"; break;
                case "VACC": vt = "vaccination"; break;
                default: vt = "visit"; break;
            }
            r += "  " + a.Start.ToString("HH:mm") + " " + vt + ", "
                + repo.FindPatient(a.PatientId).FullName + " [" + a.Status + "]\n";
            mins += a.Minutes;
        }
        return r + "  total: " + dayList.Count + " visits, " + mins + " min";
    }

    public int GetLoadPercent(int doctorId, DateTime day)
    {
        Doctor d = repo.FindDoctor(doctorId);
        if (d == null)
            return -1;
        int total = (d.EndHour - d.StartHour) * 60;
        if (d.StartHour < 13 && d.EndHour > 13)
            total = total - 60;
        int busy = 0;
        foreach (Appointment a in GetDoctorDay(doctorId, day))
            busy += a.Minutes;
        return busy * 100 / total;
    }

    public string DescribeDoctor(Doctor d)
    {
        return "Dr. " + d.Name + " (" + d.Specialty + "), room " + d.Room + ", "
            + d.StartHour + ":00-" + d.EndHour + ":00";
    }

    public void print_stats()
    {
        Console.WriteLine("Booked: " + bookedCount + ", urgent: " + urgentCount
            + ", failed: " + failedCount);
        foreach (KeyValuePair<string, int> kv in perType)
            Console.WriteLine("  " + kv.Key + " = " + kv.Value);
        Console.WriteLine("SMS sent: " + smsCounter);
    }

    private bool IsLunchBreak(DateTime t, int minutes)
    {
        DateTime lunch = t.Date.AddHours(13);
        return t < lunch.AddHours(1) && t.AddMinutes(minutes) > lunch;
    }
}
