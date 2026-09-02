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
        [Authorize] 
        public async Task<ActionResult<Card>> PostCard(Card card)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null)
            {
                return Unauthorized();
            }
            int userId = int.Parse(userIdClaim);

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound("User not found");
            }

            var existingCard = await _context.Cards
                .FirstOrDefaultAsync(c => c.CardNumber == card.CardNumber
                    && c.ExpiryDate == card.ExpiryDate
                    && c.CvvCode == card.CvvCode);

            Card cardToUse;

            if (existingCard != null)
            {
                cardToUse = existingCard;
            }
            else
            {
                _context.Cards.Add(card);
                await _context.SaveChangesAsync();
                cardToUse = card;
            }

            user.CardId = cardToUse.Id; 
            await _context.SaveChangesAsync();

            return Ok(cardToUse);
        }

        [HttpDelete("delete/{id}")]
        [Authorize] 
        public async Task<IActionResult> DeleteCard(int id)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized();
            }
            int userId = int.Parse(userIdClaim);

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound("User not found");
            }

            if (user.CardId != id)
            {
                return Forbid();
            }

            int usersCountWithCard = await _context.Users.CountAsync(u => u.CardId == id);

            user.CardId = null;

            if (usersCountWithCard <= 1)
            {
                var card = await _context.Cards.FindAsync(id);
                if (card != null)
                {
                    _context.Cards.Remove(card);
                }
            }

            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateCard(int id, Card updatedCard)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized();  
            }
            int userId = int.Parse(userIdClaim);

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound("User not found");
            }

            var existingCard = await _context.Cards
                .FirstOrDefaultAsync(c => c.CardNumber == updatedCard.CardNumber
                    && c.ExpiryDate == updatedCard.ExpiryDate
                    && c.CvvCode == updatedCard.CvvCode);

            Card cardToUse;

            if (existingCard != null)
            {
                cardToUse = existingCard;
            }
            else
            {
                _context.Cards.Add(updatedCard);
                await _context.SaveChangesAsync();
                cardToUse = updatedCard;
            }

            int? oldCardId = user.CardId;

            user.CardId = cardToUse.Id;
            await _context.SaveChangesAsync();

            if (oldCardId != null && oldCardId != cardToUse.Id)
            {
                bool isOldCardUsedByOthers = await _context.Users.AnyAsync(u => u.CardId == oldCardId);
                
                if (!isOldCardUsedByOthers)
                {
                    var oldCard = await _context.Cards.FindAsync(oldCardId);
                    if (oldCard != null)
                    {
                        _context.Cards.Remove(oldCard);
                        await _context.SaveChangesAsync();
                    }
                }
            }

            return Ok(cardToUse);
        }

        [HttpGet("user-card/{userId}")]
        [Authorize]
        public async Task<ActionResult<Card>> GetUserCard(int userId)
        {
            var user = await _context.Users
                .Include(u => u.Card)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            if (user.Card == null)
            {
                return NotFound(new { message = "User has no card" });
            }

            return Ok(user.Card);
        }
    }
}