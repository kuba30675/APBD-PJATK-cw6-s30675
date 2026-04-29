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
    
}