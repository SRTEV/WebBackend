using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransportApi.Models;

namespace TransportApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RewardTypeController : ControllerBase
    {
        private readonly AppDbContext _context;
        public RewardTypeController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/RewardType
        [HttpGet]
        public async Task<ActionResult<IEnumerable<RewardType>>> GetRewardTypes()
        {
            var rewardTypes = await _context.RewardTypes.ToListAsync();

            return Ok(rewardTypes);
        }

        // GET: api/RewardType/5
        [HttpGet("{id}")]
        public async Task<ActionResult<RewardType>> GetRewardType(int id)
        {
            var rewardType = await _context.RewardTypes.FindAsync(id);

            if (rewardType == null)
            {
                return NotFound();
            }

            return Ok(rewardType);
        }

        // POST: api/RewardType
        [HttpPost]
        public async Task<ActionResult<RewardType>> PostRewardType(RewardType rewardType)
        {
            _context.RewardTypes.Add(rewardType);
            
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetRewardType), new { id = rewardType.Id }, rewardType);
        }
    }
}