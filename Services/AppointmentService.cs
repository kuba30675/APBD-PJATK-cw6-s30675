using APBD_cw6.DTO;
using Microsoft.Data.SqlClient;

namespace APBD_cw6.Services;

public class AppointmentService : IAppointmentService
{
    private readonly string _connectionString;
    
    public AppointmentService(IConfiguration configuration)
    {
        this._connectionString = configuration.GetConnectionString("Default");
    }
    
    public async Task<AppointmentDetailsDto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        AppointmentDetailsDto? res = null;
        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(
            """
            SELECT IdAppointment, IdPatient, IdDoctor, AppointmentDate, Status, Reason, InternalNotes, CreatedAt 
            FROM Appointments
            WHERE IdAppointment = @Id
            """,
            connection
        );
        command.Parameters.AddWithValue("@Id", id);
        await connection.OpenAsync(cancellationToken);

        var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            res = new AppointmentDetailsDto
            {
                Id = reader.GetInt32(0),
                IdPatient = reader.GetInt32(1),
                IdDoctor = reader.GetInt32(2),
                AppointmentDate = reader.GetDateTime(3),
                Status = reader.GetString(4),
                Reason = reader.GetString(5),
                InternalNotes = reader.IsDBNull(6) ? null : reader.GetString(6),
                CreatedAt = reader.GetDateTime(7)
            };
        }

        if (res is null)
        {
            throw new Exception($"Nie ma w bazie pracownika o ID: {id}");
        }

        return res;
    }

    public async Task<AppointmentListDto> GetAllAsync(int? idPatient, int? idDoctor, DateTime? appointmentDate,
        CancellationToken cancellationToken = default)
    {
        var resultList = new List<AppointmentDto>();
        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(
            """
            SELECT IdAppointment, IdPatient, IdDoctor, AppointmentDate
            FROM Appointments
            // tutaj trzeba zwrocic polaczona tabele patients z appointments
            """
            );
    }

    public Task<AppointmentDetailsDto> AddAsync(CreateAppointmentRequestDto dto, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task RemoveAsync(int id, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task UpdateAsync(int id, UpdateAppointmentRequestDto dto, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}