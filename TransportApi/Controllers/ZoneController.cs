using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransportApi.Models;

namespace TransportApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ZoneController : ControllerBase
    {
        private readonly AppDbContext _context;
        public ZoneController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Zone
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Zone>>> GetZones()
        {
            return await _context.Zones.ToListAsync();
        }

        // GET: api/Zone/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Zone>> GetZone(int id)
        {
            var zone = await _context.Zones.FindAsync(id);

            if (zone == null)
            {
                return NotFound();
            }

            return zone;
        }

        // POST: api/Zone
        [HttpPost]
        public async Task<ActionResult<Zone>> PostZone(Zone zone)
        {
            _context.Zones.Add(zone);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetZone", new { id = zone.Id }, zone);
        }
    }
}