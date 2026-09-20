using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransportApi.Models;
using Microsoft.AspNetCore.Authorization;
namespace TransportApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VehicleController : ControllerBase
    {
        private readonly AppDbContext _context;

        public VehicleController(AppDbContext context)
        {
            _context = context;
        }


        // GET: api/Vehicle
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Vehicle>>> GetVehicle()
        {
            var vehicles = await _context.Vehicles
                .Include(v => v.VehicleType) 
                .Include(v => v.VehicleStatus)
                .Where(v => v.Deleted == null || v.Deleted == false)
                .ToListAsync();

            return Ok(vehicles);
        }


        // GET: api/Vehicle/5
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<Vehicle>> GetVehicle(int id)
        {
            var vehicle = await _context.Vehicles.FindAsync(id);

            if (vehicle == null || vehicle.Deleted == true)
            {
                return NotFound();
            }

            return Ok(vehicle);
        }

        // POST: api/Vehicle
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<Vehicle>> PostVehicle(Vehicle vehicle)
        {
            vehicle.VehicleType = null;
            vehicle.VehicleStatus = null;

            _context.Vehicles.Add(vehicle);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetVehicle), new { id = vehicle.Id }, vehicle);
        }

        // PUT: api/Vehicle/5
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> PutVehicle(int id, Vehicle vehicle)
        {
            if (id != vehicle.Id)
            {
                return BadRequest("ID mismatch");
            }

            vehicle.VehicleType = null;
            vehicle.VehicleStatus = null;

            _context.Entry(vehicle).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!VehicleExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // DELETE: api/Vehicle/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteVehicle(int id)
        {
            var vehicle = await _context.Vehicles.FindAsync(id);
            if (vehicle == null || vehicle.Deleted == true)
            {
                return NotFound();
            }

            vehicle.Deleted = true;
            
            _context.Entry(vehicle).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return NoContent();
        }
[HttpGet("scan/{QrCode}")]
[Authorize]
public async Task<ActionResult<Vehicle>> ScanVehicle(string QrCode)
{
    var vehicle = await _context.Vehicles
        .Include(v => v.VehicleStatus) 
        .Include(v => v.VehicleType)   
        .FirstOrDefaultAsync(v => v.QrCode == QrCode && (v.Deleted == null || v.Deleted == false));

    if (vehicle == null)
    {
        return NotFound();
    }

    return Ok(vehicle);
}

[HttpPut("inremont/{vehicleId}")]
[Authorize(Roles = "Repairman")]
public async Task<ActionResult<Vehicle>> InRemont(int vehicleId)
{
    var vehicle = await _context.Vehicles
        .FirstOrDefaultAsync(v => v.Id == vehicleId && (v.Deleted == null || v.Deleted == false));

    if (vehicle == null)
    {
        return NotFound("Vehicle not found.");
    }
    if (vehicle.VehicleStatusId == 4)
    {
        return BadRequest("Vehicle is already in repair.");
    }

    if (vehicle.VehicleStatusId != 3)
    {
        return BadRequest("Vehicle don`t need repeir.");
    }

    vehicle.VehicleStatusId = 4;
    vehicle.LastActivity = DateTime.UtcNow;

    await _context.SaveChangesAsync();


    return Ok("Vehicle status updated to 'In Repair'.   ");
}
[HttpPut("endremont/{vehicleId}")]
[Authorize(Roles = "Repairman")]
public async Task<ActionResult<Vehicle>> EndRemont(int vehicleId)
{
    var vehicle = await _context.Vehicles
        .FirstOrDefaultAsync(v => v.Id == vehicleId && (v.Deleted == null || v.Deleted == false));

    if (vehicle == null)
    {
        return NotFound("Vehicle not found.");
    }
    if (vehicle.VehicleStatusId != 4)
    {
        return BadRequest("Vehicle is not in repair.");
    }

    vehicle.VehicleStatusId = 1;
    vehicle.LastActivity = DateTime.UtcNow;

    await _context.SaveChangesAsync();



    return Ok("Vehicle status updated to 'Available'.   ");
}
        private bool VehicleExists(int id)
        {
            return _context.Vehicles.Any(e => e.Id == id && (e.Deleted == null || e.Deleted == false));
        }
    }
}