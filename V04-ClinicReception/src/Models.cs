namespace ClinicReception;

public enum AppointmentStatus { Booked, Confirmed, Completed, Cancelled, NoShow }

public class Patient
{
    public int Id;
    public string FullName;
    public string Phone;            // як ввели на рецепції
    public int BirthYear;
    public string InsuranceNo = ""; // "" - без страховки
    public string Email;
}

// HR export: working hours are whole hours only
public class Doctor
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Specialty { get; set; }
    public int Room { get; set; }
    public int StartHour { get; set; }
    public int EndHour { get; set; }
}

public class Appointment
{
    public int Id;
    public int PatientId;
    public int DoctorId;
    public string VisitType;    // CONS, FUP, PROC, VACC
    public DateTime Start;
    public int Minutes;
    public bool Urgent;
    public AppointmentStatus Status;
    public decimal Fee;
}
