namespace APBD_cw6.DTO;

public class AppointmentDto
{
    public int Id { get; set; }
    public int IdPatient { get; set; }
    public int IdDoctor { get; set; }
    public DateTime AppointmentDate { get; set; }
    public string PatientFirstName { get; set; }
    public string PatientLastName { get; set; }
    public string PatientEmail { get; set; }
    public DateTime PatientDateOfBirth { get; set; }
}