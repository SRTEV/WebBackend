using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransportApi.Models;

namespace TransportApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RouteHistoryController : ControllerBase
    {
        private readonly AppDbContext _context;

        public RouteHistoryController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/RouteHistory
        [HttpGet]
        public async Task<ActionResult<IEnumerable<RouteHistory>>> GetRouteHistories()
        {
            var routeHistories = await _context.RouteHistories
                .Include(rh => rh.Vehicle) 
                .ToListAsync();

            return Ok(routeHistories);
        }

        // GET: api/RouteHistory/5
        [HttpGet("{id}")]
        public async Task<ActionResult<RouteHistory>> GetRouteHistory(int id)
        {
            var routeHistory = await _context.RouteHistories.FindAsync(id);

            if (routeHistory == null)
            {
                return NotFound();
            }

            return Ok(routeHistory);
        }

        // POST: api/RouteHistory
        [HttpPost]
        public async Task<ActionResult<RouteHistory>> PostRouteHistory(RouteHistory routeHistory)
        {
            _context.RouteHistories.Add(routeHistory);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetRouteHistory), new { id = routeHistory.Id }, routeHistory);
        }
    }
}