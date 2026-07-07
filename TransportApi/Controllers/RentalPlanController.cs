using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransportApi.Models;

namespace TransportApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RentalPlanController : ControllerBase
    {
        private readonly AppDbContext _context;
        public RentalPlanController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/RentalPlan
        [HttpGet]
        public async Task<ActionResult<IEnumerable<RentalPlan>>> GetRentalPlans()
        {
            var rentalPlans = await _context.RentalPlans.ToListAsync();

            return Ok(rentalPlans);
        }

        // GET: api/RentalPlan/5
        [HttpGet("{id}")]
        public async Task<ActionResult<RentalPlan>> GetRentalPlan(int id)
        {
            var rentalPlan = await _context.RentalPlans.FindAsync(id);

            if (rentalPlan == null)
            {
                return NotFound();
            }

            return Ok(rentalPlan);
        }

        // POST: api/RentalPlan
        [HttpPost]
        public async Task<ActionResult<RentalPlan>> PostRentalPlan(RentalPlan rentalPlan)
        {
            _context.RentalPlans.Add(rentalPlan);
            
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetRentalPlan), new { id = rentalPlan.Id }, rentalPlan);
        }
    }
}