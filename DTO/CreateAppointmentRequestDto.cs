using System.ComponentModel.DataAnnotations;

namespace APBD_cw6.DTO;

public class CreateAppointmentRequestDto
{
    [Required] public int IdPatient { get; set; }
    [Required] public int IdDoctor { get; set; }
    [Required] public DateTime AppointmentDate { get; set; }
    [Required, MinLength(4)] string Status { get; set; } = string.Empty;

    [Required, Length(minimumLength: 5, maximumLength: 100)]
    public string Reason { get; set; } = string.Empty;

    public string? InternalNotes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}