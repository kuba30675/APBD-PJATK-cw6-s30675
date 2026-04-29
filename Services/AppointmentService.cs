using APBD_cw6.DTO;
using APBD_cw6.Exceptions;
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
            SELECT a.IdAppointment, a.AppointmentDate, a.Status, a.Reason, a.InternalNotes, a.CreatedAt,
                p.FirstName, p.LastName, p.DateOfBirth, d.FirstName, d.LastName
            FROM Appointments a 
            INNER JOIN Doctors d ON a.IdDoctor = d.IdDoctor
            INNER JOIN Patients p ON a.IdPatient = p.IdPatient
            WHERE a.IdAppointment = @Id
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
                AppointmentDate = reader.GetDateTime(1),
                Status = reader.GetString(2),
                Reason = reader.GetString(3),
                InternalNotes = reader.IsDBNull(4) ? null : reader.GetString(4),
                CreatedAt = reader.GetDateTime(5),
                PatientFirstName = reader.GetString(6),
                PatientLastName = reader.GetString(7),
                PatientDateOfBirth = DateOnly.FromDateTime(reader.GetDateTime(8)),
                DoctorFirstName = reader.GetString(9),
                DoctorLastName = reader.GetString(10)
            };
        }

        if (res is null)
        {
            throw new Exception($"Nie ma wizyty o ID: {id}");
        }

        return res;
    }

    public async Task<IEnumerable<AppointmentDto>> GetAllAsync(int? idPatient, int? idDoctor, DateTime? appointmentDate,
        string? status, string? patientLastName, CancellationToken cancellationToken = default)
    {
        var res = new List<AppointmentDto>();
        var query = """
                    SELECT a.IdAppointment, a.AppointmentDate, a.Reason, a.Status,
                           p.FirstName, p.LastName, p.Email, p.DateOfBirth
                    FROM Appointments a
                    INNER JOIN Patients p ON a.IdPatient = p.IdPatient
                    """;
        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand();
        var conditions = new List<string>();

        if (idPatient.HasValue)
        {
            conditions.Add("a.IdPatient = @IdPatient");
            command.Parameters.AddWithValue("@IdPatient", idPatient);
        }

        if (idDoctor.HasValue)
        {
            conditions.Add("a.IdDoctor = @IdDoctor");
            command.Parameters.AddWithValue("@IdDoctor", idDoctor);
        }

        if (appointmentDate.HasValue)
        {
            conditions.Add("a.AppointmentDate = @AppointmentDate");
            command.Parameters.AddWithValue("@AppointmentDate", appointmentDate);
        }

        if (!string.IsNullOrEmpty(status))
        {
            conditions.Add("a.Status = @Status");
            command.Parameters.AddWithValue("@Status", status);
        }

        if (!string.IsNullOrEmpty(patientLastName))
        {
            conditions.Add("p.LastName = @PatientLastName");
            command.Parameters.AddWithValue("@PatientLastName", patientLastName);
        }

        if (conditions.Any())
            query += " WHERE " + string.Join(" AND ", conditions);

        command.CommandText = query;
        command.Connection = connection;
        await connection.OpenAsync(cancellationToken);

        var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            var appointment = new AppointmentDto
            {
                Id = reader.GetInt32(0),
                AppointmentDate = reader.GetDateTime(1),
                Reason = reader.GetString(2),
                Status = reader.GetString(3),
                PatientFirstName = reader.GetString(4),
                PatientLastName = reader.GetString(5),
                PatientEmail = reader.GetString(6),
                PatientDateOfBirth = DateOnly.FromDateTime(reader.GetDateTime(7))
            };
            res.Add(appointment);
        }

        if (res.Count < 1)
            throw new NotFoundException("Nie ma wizyty dla podanych parametrow!");

        return res;
    }

    public async Task AddAsync(CreateAppointmentRequestDto dto, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(
            """
                SELECT IsActive
                FROM Patients
                WHERE IdPatient = @IdPatient
            """,
            connection);
        command.Parameters.AddWithValue("@IdPatient", dto.IdPatient);
        await connection.OpenAsync(cancellationToken);
        var patientResult = await command.ExecuteScalarAsync(cancellationToken);

        if (patientResult is not null)
        {
            var isPatientActive = Convert.ToBoolean(patientResult);
            if (!isPatientActive)
                throw new PatientNotActiveException($"Pacjent o ID: {dto.IdPatient} nie jest aktywny!");
        }
        else
        {
            throw new NotFoundException($"Nie ma pacjenta o ID: {dto.IdPatient}");
        }

        command.Parameters.Clear();
        command.CommandText = """
                                SELECT IsActive
                                FROM Doctors
                                WHERE IdDoctor = @IdDoctor
                              """;
        command.Parameters.AddWithValue("@IdDoctor", dto.IdDoctor);
        var doctorResult = await command.ExecuteScalarAsync(cancellationToken);

        if (doctorResult is not null)
        {
            var isDoctorActive = Convert.ToBoolean(doctorResult);
            if (!isDoctorActive)
                throw new DoctorNotActiveException($"Doktor o ID: {dto.IdDoctor} nie jest aktywny!");
        }
        else
        {
            throw new NotFoundException($"Nie ma doktora o ID: {dto.IdDoctor}");
        }

        if (dto.AppointmentDate < DateTime.Now)
            throw new AppointmentDateInPastException("Data utworzonej wizyty nie moze byc w przeszlosci");

        command.Parameters.Clear();
        command.CommandText = """
                              SELECT 1 
                              FROM Appointments
                              WHERE IdDoctor = @IdDoctor
                              AND AppointmentDate >= @From
                              AND AppointmentDate < @To
                              """;
        command.Parameters.AddWithValue("@IdDoctor", dto.IdDoctor);
        var from = dto.AppointmentDate.Date;
        var to = from.AddDays(1);
        command.Parameters.AddWithValue("@From", from);
        command.Parameters.AddWithValue("@To", to);
        var result = await command.ExecuteScalarAsync(cancellationToken);

        if (result is not null)
            throw new DoctorBusyException(
                $"Doktor o ID: {dto.IdDoctor} juz ma zaplanowana wizyte na dzien {dto.AppointmentDate}");


        command.CommandText = """
                              INSERT INTO Appointments
                              VALUES(@IdPatient,@IdDoctor,@AppointmentDate,@Status,@Reason,@InternalNotes,@CreatedAt)
                              """;
        command.Parameters.AddWithValue("@IdPatient", dto.IdPatient);
        command.Parameters.AddWithValue("@AppointmentDate", dto.AppointmentDate);
        command.Parameters.AddWithValue("@Reason", dto.Reason);
        command.Parameters.AddWithValue("@InternalNotes",
            string.IsNullOrEmpty(dto.InternalNotes) ? DBNull.Value : dto.InternalNotes);
        command.Parameters.AddWithValue("@CreatedAt", dto.CreatedAt);
        command.Parameters.AddWithValue("@Status", "Scheduled");

        int queryResult = await command.ExecuteNonQueryAsync(cancellationToken);

        if (queryResult == 0)
        {
            throw new Exception("Wizyta nie zostala utworzona!");
        }
    }

    public async Task RemoveAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(
            """
                SELECT Status 
                FROM Appointments 
                WHERE IdAppointment = @IdAppointment
            """,
            connection
        );

        command.Parameters.AddWithValue("@IdAppointment", id);
        await connection.OpenAsync(cancellationToken);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        if (result is null)
        {
            throw new NotFoundException($"Nie ma wizyty o ID: {id}");
        }

        var appointmentStatus = Convert.ToString(result);
        if (appointmentStatus == "Completed")
        {
            throw new AppointmentCompletedException($"Wizyta o ID: {id} ma status Completed. Nie mozna usunac!");
        }

        command.Parameters.Clear();
        command.CommandText = """
                              DELETE 
                              FROM Appointments
                              WHERE IdAppointment = @IdAppointment
                              """;

        command.Parameters.AddWithValue("@IdAppointment", id);
        var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);
        if (rowsAffected == 0)
        {
            throw new Exception("Nie udalo sie usunac wizyty");
        }
    }

    public async Task UpdateAsync(int id, UpdateAppointmentRequestDto dto,
        CancellationToken cancellationToken = default)
    {
        if (dto.Status == "Scheduled" || dto.Status == "Completed" || dto.Status == "Cancelled")
        {
        }
        else
        {
            throw new IllegalStatusException($"Status {dto.Status} nie jest dopuszczany");
        }

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(
            """
                SELECT IsActive
                FROM Patients
                WHERE IdPatient = @IdPatient
            """,
            connection);
        command.Parameters.AddWithValue("@IdPatient", dto.IdPatient);
        await connection.OpenAsync(cancellationToken);
        var patientResult = await command.ExecuteScalarAsync(cancellationToken);

        if (patientResult is not null)
        {
            var isPatientActive = Convert.ToBoolean(patientResult);
            if (!isPatientActive)
                throw new PatientNotActiveException($"Pacjent o ID: {dto.IdPatient} nie jest aktywny!");
        }
        else
        {
            throw new NotFoundException($"Nie ma pacjenta o ID: {dto.IdPatient}");
        }

        command.Parameters.Clear();
        command.CommandText = """
                                SELECT IsActive
                                FROM Doctors
                                WHERE IdDoctor = @IdDoctor
                              """;
        command.Parameters.AddWithValue("@IdDoctor", dto.IdDoctor);
        var doctorResult = await command.ExecuteScalarAsync(cancellationToken);

        if (doctorResult is not null)
        {
            var isDoctorActive = Convert.ToBoolean(doctorResult);
            if (!isDoctorActive)
                throw new DoctorNotActiveException($"Doktor o ID: {dto.IdDoctor} nie jest aktywny!");
        }
        else
        {
            throw new NotFoundException($"Nie ma doktora o ID: {dto.IdDoctor}");
        }

        command.Parameters.Clear();
        command.CommandText = """
                              SELECT 1 
                              FROM Appointments
                              WHERE IdAppointment = @IdAppointment
                              """;
        command.Parameters.AddWithValue("@IdAppointment", id);
        var appointmentResult = await command.ExecuteScalarAsync(cancellationToken);

        if (appointmentResult is null)
        {
            throw new NotFoundException($"Nie ma wizyty o ID: {id}");
        }

        command.Parameters.Clear();

        if (dto.Status != "Completed")
        {
            command.CommandText = """
                                  UPDATE Appointments
                                  SET IdPatient = @IdPatient, IdDoctor = @IdDoctor, AppointmentDate = @AppointmentDate,
                                      Status = @Status, Reason = @Reason, InternalNotes = @InternalNotes
                                  WHERE IdAppointment = @IdAppointment
                                  """;
            command.Parameters.AddWithValue("@AppointmentDate", dto.AppointmentDate);
        }
        else
        {
            command.CommandText = """
                                  UPDATE Appointments
                                  SET IdPatient = @IdPatient, IdDoctor = @IdDoctor,Status = @Status, 
                                      Reason = @Reason, InternalNotes = @InternalNotes
                                  WHERE IdAppointment = @IdAppointment
                                  """;   
        }

        command.Parameters.AddWithValue("@IdPatient", dto.IdPatient);
        command.Parameters.AddWithValue("@IdDoctor", dto.IdDoctor);
        command.Parameters.AddWithValue("@Status", dto.Status);
        command.Parameters.AddWithValue("@Reason", dto.Reason);
        command.Parameters.AddWithValue("@InternalNotes",
            string.IsNullOrEmpty(dto.InternalNotes) ? DBNull.Value : dto.InternalNotes);
        command.Parameters.AddWithValue("@IdAppointment", id);

        var affectedRows = await command.ExecuteNonQueryAsync(cancellationToken);

        if (affectedRows == 0)
        {
            throw new Exception("Nie udalo sie zaaktualizowac wiersza rezerwacji");
        }
    }
}