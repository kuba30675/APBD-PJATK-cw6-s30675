namespace APBD_cw6.Services;
using APBD_cw6.DTO;

public interface IAppointmentService
{
    Task<IEnumerable<AppointmentDto>> GetAllAsync(int? idPatient, int? idDoctor, DateTime? appointmentDate, string? status,
        string? patientLastName, CancellationToken cancellationToken = default);
    Task<AppointmentDetailsDto> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task AddAsync(CreateAppointmentRequestDto dto,
        CancellationToken cancellationToken = default);

    Task RemoveAsync(int id, CancellationToken cancellationToken = default);
    Task UpdateAsync(int id, UpdateAppointmentRequestDto dto, CancellationToken cancellationToken = default);
}