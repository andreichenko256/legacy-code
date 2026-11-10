namespace ClinicReception;

// Поки без бази: одна копія на всю програму
public class ClinicRepository
{
    public static ClinicRepository Current = new ClinicRepository();
    private static int lastId = 1000;

    public List<Patient> Patients = new List<Patient>();
    public List<Doctor> Doctors = new List<Doctor>();
    public List<Appointment> Appointments = new List<Appointment>();

    // номери записів наскрізні, як у журналі
    public static int NextId()
    {
        lastId++;
        return lastId;
    }

    public Patient FindPatient(int id)
    {
        foreach (Patient p in Patients)
            if (p.Id == id)
                return p;
        return null;
    }

    public Doctor FindDoctor(int id)
    {
        for (int i = 0; i < Doctors.Count; i++)
            if (Doctors[i].Id == id) return Doctors[i];
        return null;
    }

    public Appointment FindAppointment(int id)
    {
        return Appointments.FirstOrDefault(a => a.Id == id);
    }

    // щеплення (VACC) не займає графік пацієнта
    public List<Appointment> FindForPatient(int patientId, DateTime day)
    {
        List<Appointment> res = new List<Appointment>();
        foreach (Appointment a in Appointments)
            if (a.PatientId == patientId && a.Start.Date == day.Date
                && a.Status != AppointmentStatus.Cancelled && a.VisitType != "VACC")
                res.Add(a);
        return res;
    }

    public void AddDoctor(int id, string name, string specialty, int room, int startHour,
        int endHour)
    {
        Doctors.Add(new Doctor { Id = id, Name = name, Specialty = specialty, Room = room,
            StartHour = startHour, EndHour = endHour });
    }
}
