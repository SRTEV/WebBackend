using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransportApi.Models;

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