namespace APBD_cw6.Services;
using APBD_cw6.DTO;

public interface IAppointmentService
{
    Task<AppointmentListDto> GetAllAsync(int? idPatient, int? idDoctor, DateTime? appointmentDate, CancellationToken cancellationToken = default);
    Task<AppointmentDetailsDto> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<AppointmentDetailsDto> AddAsync(CreateAppointmentRequestDto dto,
        CancellationToken cancellationToken = default);

    Task RemoveAsync(int id, CancellationToken cancellationToken = default);
    Task UpdateAsync(int id, UpdateAppointmentRequestDto dto, CancellationToken cancellationToken = default);
}