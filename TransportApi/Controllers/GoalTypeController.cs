using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransportApi.Models;

namespace TransportApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GoalTypeController : ControllerBase
    {
        private readonly AppDbContext _context;
        public GoalTypeController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/GoalType
        [HttpGet]
        public async Task<ActionResult<IEnumerable<GoalType>>> GetGoalTypes()
        {
            var goalTypes = await _context.GoalTypes.ToListAsync();

            return Ok(goalTypes);
        }

        // GET: api/GoalType/5
        [HttpGet("{id}")]
        public async Task<ActionResult<GoalType>> GetGoalType(int id)
        {
            var goalType = await _context.GoalTypes.FindAsync(id);

            if (goalType == null)
            {
                return NotFound();
            }

            return Ok(goalType);
        }

        // POST: api/GoalType
        [HttpPost]
        public async Task<ActionResult<GoalType>> PostGoalType(GoalType goalType)
        {
            _context.GoalTypes.Add(goalType);
            
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetGoalType), new { id = goalType.Id }, goalType);
        }
    }
}