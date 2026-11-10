namespace ClinicReception;

public class ReceptionDesk
{
    private ClinicRepository repo = ClinicRepository.Current;
    public string LastMessage = "";

    // повертає id пацієнта або -1, якщо дані некоректні
    public int RegisterPatient(string fullName, string phone, int birthYear, string insuranceNo,
        string email)
    {
        LastMessage = "";
        if (fullName == null || fullName.Trim().Length < 3)
            LastMessage = "Name is too short";
        else if (phone == null || phone.Length < 10)
            LastMessage = "Phone number is too short";
        else if (birthYear < 1900 || birthYear > DateTime.Now.Year)
            LastMessage = "Wrong birth year";
        if (LastMessage != "")
            return -1;
        Patient p = new Patient { Id = repo.Patients.Count + 1, FullName = fullName.Trim(),
            Phone = phone, BirthYear = birthYear, InsuranceNo = insuranceNo, Email = email };
        repo.Patients.Add(p);
        AuditLog.Write("NEW PATIENT " + p.Id);
        return p.Id;
    }

    // action приходить з кнопок форми: confirm, complete, cancel, noshow
    public bool ChangeStatus(int appointmentId, string action)
    {
        Appointment a = repo.FindAppointment(appointmentId);
        if (a == null)
        {
            LastMessage = "Appointment #" + appointmentId + " not found";
            return false;
        }
        AppointmentStatus old = a.Status;
        switch (action)
        {
            case "confirm":
                if (a.Status == AppointmentStatus.Booked)
                    a.Status = AppointmentStatus.Confirmed;
                break;
            case "complete":
                if (a.Status == AppointmentStatus.Cancelled)
                    throw new InvalidOperationException("Cannot complete a cancelled visit");
                if (a.Status == AppointmentStatus.Confirmed)
                    a.Status = AppointmentStatus.Completed;
                break;
            case "cancel":
                if (a.Status == AppointmentStatus.Booked || a.Status == AppointmentStatus.Confirmed)
                    a.Status = AppointmentStatus.Cancelled;
                break;
            case "noshow":
                if (a.Status == AppointmentStatus.Confirmed && a.Start < DateTime.Now)
                    a.Status = AppointmentStatus.NoShow;
                break;
        }
        // статус не змінився = перехід заборонений
        if (a.Status == old)
        {
            LastMessage = "Cannot " + action + " appointment #" + a.Id + " in status " + old;
            return false;
        }
        LastMessage = "Appointment #" + a.Id + ": " + old + " -> " + a.Status;
        AuditLog.Write(LastMessage);
        return true;
    }
}
