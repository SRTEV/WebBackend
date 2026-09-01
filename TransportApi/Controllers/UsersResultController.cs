using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransportApi.Models;
using Microsoft.AspNetCore.Authorization;
namespace TransportApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersResultController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UsersResultController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/UsersResult
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UsersResult>>> GetUsersResults()
        {
            return await _context.UsersResults.ToListAsync();
        }

        // GET: api/UsersResult/5
        [HttpGet("{id}")]
        public async Task<ActionResult<UsersResult>> GetUsersResult(int id)
        {
            var usersResult = await _context.UsersResults.FindAsync(id);

            if (usersResult == null)
            {
                return NotFound();
            }

            return usersResult;
        }

        // GET: api/UsersResult/leaderboard/5 
        [HttpGet("leaderboard/{competitionId}")]
        [Authorize]
        public async Task<IActionResult> GetLeaderboard(int competitionId)
        {
            var results = await _context.UsersResults
                .Where(ur => ur.CompetitionId == competitionId)
                .Include(ur => ur.User)
                .OrderByDescending(ur => ur.Score) 
                .ToListAsync();

            if (results == null || !results.Any())
            {
                return NotFound(new { message = "No participants yet" });
            }

            int rank = 1;
            var leaderboard = results.Select(ur => new
            {
                Rank = rank++,
                UserId = ur.UserId,
                Name = ur.User?.Name ?? "Unknown",
                Score = ur.Score
            });

            return Ok(leaderboard);
        }



        // POST: api/UsersResult
        [HttpPost]
        public async Task<ActionResult<UsersResult>> PostUsersResult(UsersResult usersResult)
        {
            _context.UsersResults.Add(usersResult);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetUsersResult", new { id = usersResult.Id }, usersResult);
        }
    }
}