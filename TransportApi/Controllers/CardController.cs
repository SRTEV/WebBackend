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

        [HttpGet]
        public async Task<ActionResult<IEnumerable<TransportApi.Models.Card>>> GetCards()
        {
            var cards = await _context.Cards.ToListAsync();
            return Ok(cards);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<TransportApi.Models.Card>> GetCard(int id)
        {
            var card = await _context.Cards.FindAsync(id);
            if (card == null) return NotFound();
            return Ok(card);
        }

        [HttpPost]
        [Authorize] 
        public async Task<ActionResult<TransportApi.Models.Card>> PostCard(TransportApi.Models.Card card)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null) return Unauthorized();
            
            int userId = int.Parse(userIdClaim);
            var user = await _context.Users
                .Include(u => u.Card)
                .FirstOrDefaultAsync(u => u.Id == userId);
                
            if (user == null) return NotFound("User not found");

            // Якщо у користувача вже є картка, оновлюємо її замість створення дублікатів
            if (user.Card != null)
            {
                user.Card.CardNumber = card.CardNumber;
                user.Card.ExpiryDate = card.ExpiryDate;
                user.Card.CvvCode = card.CvvCode; // Зберігаємо справжній CVV або передані дані
                
                _context.Cards.Update(user.Card);
                await _context.SaveChangesAsync();
                return Ok(user.Card);
            }
            else
            {
                // Створюємо новий запис картки спеціально для цього користувача
                _context.Cards.Add(card);
                await _context.SaveChangesAsync();

                user.CardId = card.Id;
                await _context.SaveChangesAsync();

                return Ok(card);
            }
        }

        [HttpDelete("delete/{id}")]
        [Authorize] 
        public async Task<IActionResult> DeleteCard(int id)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return Unauthorized();
            
            int userId = int.Parse(userIdClaim);
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound("User not found");

            if (user.CardId != id) return Forbid();

            user.CardId = null;
            
            var card = await _context.Cards.FindAsync(id);
            if (card != null)
            {
                _context.Cards.Remove(card);
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Card deleted successfully" });
        }  

        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateCard(int id, TransportApi.Models.Card updatedCard)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return Unauthorized();
            
            int userId = int.Parse(userIdClaim);
            var user = await _context.Users
                .Include(u => u.Card)
                .FirstOrDefaultAsync(u => u.Id == userId);
                
            if (user == null) return NotFound("User not found");

            if (user.Card != null)
            {
                user.Card.CardNumber = updatedCard.CardNumber;
                user.Card.ExpiryDate = updatedCard.ExpiryDate;
                user.Card.CvvCode = updatedCard.CvvCode;

                _context.Cards.Update(user.Card);
                await _context.SaveChangesAsync();
                return Ok(user.Card);
            }
            else
            {
                _context.Cards.Add(updatedCard);
                await _context.SaveChangesAsync();

                user.CardId = updatedCard.Id;
                await _context.SaveChangesAsync();

                return Ok(updatedCard);
            }
        }

        [HttpGet("user-card/{userId}")]
        [Authorize]
        public async Task<ActionResult<TransportApi.Models.Card>> GetUserCard(int userId)
        {
            var user = await _context.Users
                .Include(u => u.Card)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null) return NotFound(new { message = "User not found" });
            if (user.Card == null) return NotFound(new { message = "User has no card" });

            return Ok(user.Card);
        }
    }
}