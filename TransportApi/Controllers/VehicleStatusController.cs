using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransportApi.Models;

namespace TransportApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VehicleStatusController : ControllerBase
    {
        private readonly AppDbContext _context;
        public VehicleStatusController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/VehicleStatus
        [HttpGet]
        public async Task<ActionResult<IEnumerable<VehicleStatus>>> GetVehicleStatuses()
        {
            return await _context.VehicleStatuses.ToListAsync();
        }

        // GET: api/VehicleStatus/5
        [HttpGet("{id}")]
        public async Task<ActionResult<VehicleStatus>> GetVehicleStatus(int id)
        {
            var vehicleStatus = await _context.VehicleStatuses.FindAsync(id);

            if (vehicleStatus == null)
            {
                return NotFound();
            }

            return vehicleStatus;
        }

        // POST: api/VehicleStatus
        [HttpPost]
        public async Task<ActionResult<VehicleStatus>> PostVehicleStatus(VehicleStatus vehicleStatus)
        {
            _context.VehicleStatuses.Add(vehicleStatus);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetVehicleStatus", new { id = vehicleStatus.Id }, vehicleStatus);
        }
    }
}