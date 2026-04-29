using APBD_cw6.DTO;
using APBD_cw6.Exceptions;
using Microsoft.AspNetCore.Mvc;
using APBD_cw6.Services;
namespace APBD_cw6.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AppointmentsController(IAppointmentService service) : ControllerBase
{
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetAppointmentByIdAsync([FromRoute] int id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return Ok(await service.GetByIdAsync(id, cancellationToken));
        }
        catch (Exception e)
        {
            return NotFound(e.Message);
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetAllAsync([FromQuery] int? idPatient, int? idDoctor, DateTime? appointmentDate,
        string? status, string? patientLastName, CancellationToken cancellationToken = default
    )
    {
        try
        {
            return Ok(await service.GetAllAsync(idPatient, idDoctor, appointmentDate, status, patientLastName, cancellationToken));
        }
        catch (NotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpPost]
    public async Task<IActionResult> AddAppointmentAsync(CreateAppointmentRequestDto dto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await service.AddAsync(dto, cancellationToken);
            return Created();
        }
        catch (PatientNotActiveException ex)
        {
            return Conflict(ex.Message);
        }
        catch (NotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (DoctorNotActiveException ex)
        {
            return Conflict(ex.Message);
        }
        catch (AppointmentDateInPastException ex)
        {
            return Conflict(ex.Message);
        }
        catch (DoctorBusyException ex)
        {
            return Conflict(ex.Message);
        }
        catch (Exception ex)
        {
            return Conflict(ex.Message);
        }
        
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> RemoveAppointmentAsync([FromRoute] int id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await service.RemoveAsync(id, cancellationToken);
            return NoContent();
        }
        catch (NotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (AppointmentCompletedException ex)
        {
            return Conflict(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, "Blad po stronie serwera");
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateAppointmentAsync([FromRoute] int id, UpdateAppointmentRequestDto dto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await service.UpdateAsync(id, dto, cancellationToken);
            return NoContent();
        }
        catch (NotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (PatientNotActiveException ex)
        {
            return Conflict(ex.Message);
        }
        catch (DoctorNotActiveException ex)
        {
            return Conflict(ex.Message);
        }
        catch (IllegalStatusException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, "Blad po stronie serwera");
        }
        
    }
    
}