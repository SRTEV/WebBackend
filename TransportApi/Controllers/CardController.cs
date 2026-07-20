using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransportApi.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace TransportApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CardController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CardController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Card
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Card>>> GetCards()
        {
            var cards = await _context.Cards.ToListAsync();
            return Ok(cards);
        }

        // GET: api/Card/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Card>> GetCard(int id)
        {
            var card = await _context.Cards.FindAsync(id);

            if (card == null)
            {
                return NotFound();
            }

            return Ok(card);
        }

        // POST: api/Card
        [HttpPost]
        [Authorize] // Required to get the user ID
        public async Task<ActionResult<Card>> PostCard(Card card)
        {
            // 1. Get current user ID from the token
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            // 2. Save the card first to obtain its new ID
            _context.Cards.Add(card);
            await _context.SaveChangesAsync();

            // 3. Find the user in the DB
            var user = await _context.Users.FindAsync(userId);

            // 4. Link the card to the user
            if (user != null)
            {
                user.CardId = card.Id; // Store the new card ID in the user's CardId field
                await _context.SaveChangesAsync();
            }

            return CreatedAtAction(nameof(GetCard), new { id = card.Id }, card);
        }
        
    }
}