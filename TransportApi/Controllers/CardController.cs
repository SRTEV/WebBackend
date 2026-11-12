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

            // 1. Спершу шукаємо, чи є така картка в базі взагалі (за номером)
            var existingCardInDb = await _context.Cards
                .FirstOrDefaultAsync(c => c.CardNumber == card.CardNumber);

            if (existingCardInDb != null)
            {
                // Якщо картка вже існує в базі, просто прив'язуємо її до цього юзера
                user.CardId = existingCardInDb.Id; // (замініть .ID на .Id, якщо у вашій моделі літера 'd' маленька)
                
                // Оновлюємо дані на випадок змін
                existingCardInDb.ExpiryDate = card.ExpiryDate;
                existingCardInDb.CvvCode = card.CvvCode;

                _context.Cards.Update(existingCardInDb);
                await _context.SaveChangesAsync();
                
                return Ok(existingCardInDb);
            }

            // 2. Якщо цієї картки взагалі не було в базі, перевіряємо чи у користувача вже була інша привязана картка
            if (user.Card != null)
            {
                user.Card.CardNumber = card.CardNumber;
                user.Card.ExpiryDate = card.ExpiryDate;
                user.Card.CvvCode = card.CvvCode;
                
                _context.Cards.Update(user.Card);
                await _context.SaveChangesAsync();
                return Ok(user.Card);
            }
            else
            {
                // 3. Якщо картки немає ні в базі, ні у користувача — створюємо нову
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