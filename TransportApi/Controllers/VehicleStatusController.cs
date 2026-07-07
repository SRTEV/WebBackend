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
        public async Task<ActionResult<IEnumerable<VechicleStatus>>> GetVehicleStatuses()
        {
            // Використовуємо VechicleStatuses з одруківкою, як у контексті
            return await _context.VechicleStatuses.ToListAsync();
        }

        // GET: api/VehicleStatus/5
        [HttpGet("{id}")]
        public async Task<ActionResult<VechicleStatus>> GetVehicleStatus(int id)
        {
            var vehicleStatus = await _context.VechicleStatuses.FindAsync(id);

            if (vehicleStatus == null)
            {
                return NotFound();
            }

            return vehicleStatus;
        }

        // POST: api/VehicleStatus
        [HttpPost]
        public async Task<ActionResult<VechicleStatus>> PostVehicleStatus(VechicleStatus vehicleStatus)
        {
            _context.VechicleStatuses.Add(vehicleStatus);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetVehicleStatus", new { id = vehicleStatus.Id }, vehicleStatus);
        }
    }
}