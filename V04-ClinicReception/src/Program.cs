namespace ClinicReception;

public class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine("=== City Clinic #3: reception desk demo ===");
        ClinicRepository repo = ClinicRepository.Current;
        repo.AddDoctor(1, "Olena Shevchenko", "therapist", 12, 8, 14);
        repo.AddDoctor(2, "Taras Bondar", "surgeon", 21, 8, 13);
        repo.AddDoctor(3, "Iryna Melnyk", "cardiologist", 7, 15, 16);
        ReceptionDesk desk = new ReceptionDesk();
        desk.RegisterPatient("Petro Ivanenko", "067 123 45 67", 1948, "UA-55120", "");
        desk.RegisterPatient("Maria Koval", "(050) 765-43-21", 1985, "", "mk@ukr.net");
        desk.RegisterPatient("Andrii Tkachenko", "0931112233", 1992, "UA-90014", "");
        Console.WriteLine("Invalid patient -> " + desk.RegisterPatient("Ol", "", 1990, "", "")
            + " (" + desk.LastMessage + ")");

        SchedulePlanner planner = new SchedulePlanner();
        DateTime day = DateTime.Today.AddDays(1);
        planner.AddHoliday(2, day.AddDays(2), day.AddDays(5));
        Console.WriteLine("--- Booking: tomorrow, then holiday, yesterday, unknown patient");
        Appointment a1 = Book(planner, 1, 1, "CONS", day, false);
        Appointment a2 = Book(planner, 2, 1, "PROC", day, false);
        Appointment a3 = Book(planner, 1, 2, "FUP", day, false);
        Book(planner, 3, 1, "XRAY", day, false);
        Book(planner, 2, 3, "PROC", day, false);
        Appointment a6 = Book(planner, 2, 3, "PROC", day, true);
        Book(planner, 3, 2, "CONS", day.AddDays(3), false);
        Book(planner, 3, 1, "FUP", day.AddDays(-2), false);
        Book(planner, 42, 1, "CONS", day, false);

        Console.WriteLine("--- Status changes");
        Step(desk, a1.Id, "confirm");
        Step(desk, a1.Id, "complete");
        Step(desk, a1.Id, "confirm");
        Step(desk, a2.Id, "cancel");
        Step(desk, a2.Id, "complete");
        Step(desk, 777, "confirm");

        Console.WriteLine("--- Reschedule, invoice, end of day");
        Appointment moved = planner.Reschedule(a3.Id, day.AddDays(1));
        Console.WriteLine("Moved to #" + moved.Id + " at " + moved.Start.ToString("HH:mm"));
        Console.WriteLine(new FeeCalculator().FormatInvoice(a6, repo.FindPatient(2)));
        Console.WriteLine(planner.BuildDayReport(1, day));
        Console.WriteLine("Load: dr1 " + planner.GetLoadPercent(1, day) + "%, dr3 "
            + planner.GetLoadPercent(3, day) + "%, dr9 " + planner.GetLoadPercent(9, day));
        planner.print_stats();
    }

    private static Appointment Book(SchedulePlanner planner, int patientId, int doctorId,
        string type, DateTime day, bool urgent)
    {
        Appointment a = planner.Book(patientId, doctorId, type, day, urgent);
        string what = "p" + patientId + " -> dr" + doctorId + " " + type + (urgent ? "!" : "");
        if (a == null)
            Console.WriteLine(what + ": FAILED, " + planner.GetLastError());
        else
            Console.WriteLine(what + ": #" + a.Id + " at " + a.Start.ToString("HH:mm") + ", "
                + a.Minutes + " min, fee " + a.Fee.ToString("0.00"));
        return a;
    }

    private static void Step(ReceptionDesk desk, int id, string action)
    {
        try
        {
            bool ok = desk.ChangeStatus(id, action);
            Console.WriteLine("  " + action + " -> " + (ok ? "OK: " : "REFUSED: ")
                + desk.LastMessage);
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine("  " + action + " #" + id + " -> ERROR: " + ex.Message);
        }
    }
}
