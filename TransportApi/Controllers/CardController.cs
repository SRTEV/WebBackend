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

        // POST: api/card
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

            // 1. Шукаємо, чи є вже така картка в базі за номером
            var existingCardInDb = await _context.Cards
                .FirstOrDefaultAsync(c => c.CardNumber == card.CardNumber);

            if (existingCardInDb != null)
            {
                // Перевіряємо валідність реквізитів (захист від перезапису чужої картки з іншим CVV/терміном)
                if (existingCardInDb.ExpiryDate != card.ExpiryDate || existingCardInDb.CvvCode != card.CvvCode)
                {
                    return BadRequest(new { message = "Card already exists in the database, but different data (expiry date or CVV) is provided." });
                }

                // Якщо дані повністю збігаються (сімейна картка) — просто прив'язуємо її до юзера
                user.CardId = existingCardInDb.Id;
                _context.Users.Update(user);
                await _context.SaveChangesAsync();
                
                return Ok(existingCardInDb);
            }

            // 2. Якщо картки з таким номером немає в базі, але у юзера ВЖЕ є своя інша прив'язана картка — оновлюємо її
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
                // 3. Якщо картки немає ні в базі, ні у користувача — створюємо повністю нову
                _context.Cards.Add(card);
                await _context.SaveChangesAsync();

                user.CardId = card.Id;
                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                return Ok(card);
            }
        }

        // DELETE: api/card/delete/{id}
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
            _context.Users.Update(user);

         
            var otherUsersUsingCard = await _context.Users
                .AnyAsync(u => u.CardId == id && u.Id != userId);

            var card = await _context.Cards.FindAsync(id);
            if (card != null && !otherUsersUsingCard)
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

    // Зберігаємо ID поточної картки користувача до зміни
    int? oldCardId = user.CardId;

    // 1. Перевіряємо, чи введений номер картки вже існує в базі в когось іншого (сімейна картка)
    var existingCardInDb = await _context.Cards
        .FirstOrDefaultAsync(c => c.CardNumber == updatedCard.CardNumber);

    if (existingCardInDb != null && existingCardInDb.Id != user.CardId)
    {
        // Якщо картка існує в іншого юзера, але дані відрізняються — забороняємо
        if (existingCardInDb.ExpiryDate != updatedCard.ExpiryDate || existingCardInDb.CvvCode != updatedCard.CvvCode)
        {
            return BadRequest(new { message = "Card already exists in the database, but different data (expiry date or CVV) is provided." });
        }

        // Приєднуємося до існуючої сімейної картки
        user.CardId = existingCardInDb.Id;
        _context.Users.Update(user);

        // Перевіряємо стару картку: чи використовував її хтось інший, окрім нас
        if (oldCardId.HasValue)
        {
            var otherUsersUsingOldCard = await _context.Users
                .AnyAsync(u => u.CardId == oldCardId.Value && u.Id != userId);

            // Якщо ми були єдиним власником старої картки — видаляємо її з бази назавжди!
            if (!otherUsersUsingOldCard)
            {
                var oldCard = await _context.Cards.FindAsync(oldCardId.Value);
                if (oldCard != null)
                {
                    _context.Cards.Remove(oldCard);
                }
            }
        }

        await _context.SaveChangesAsync();
        return Ok(existingCardInDb);
    }

    // 2. Якщо це оновлення на абсолютно нову картку (або зміна власних реквізитів)
    if (user.Card != null)
    {
        var currentCardId = user.Card.Id;

        // Перевіряємо, чи використовує поточну картку хтось інший (чи є вона сімейною)
        var otherUsersUsingCard = await _context.Users
            .AnyAsync(u => u.CardId == currentCardId && u.Id != userId);

        if (otherUsersUsingCard)
        {
            // Якщо картка спільна, не чіпаємо її дані для інших учасників, а лише відв'язуємо поточного юзера
            user.CardId = null;
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            // Створюємо нову персональну картку для цього юзера
            _context.Cards.Add(updatedCard);
            await _context.SaveChangesAsync();

            user.CardId = updatedCard.Id;
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            return Ok(updatedCard);
        }
        else
        {
            // Якщо картка належала ТІЛЬКИ нам (не сімейна) — просто оновлюємо її реквізити на місці
            user.Card.CardNumber = updatedCard.CardNumber;
            user.Card.ExpiryDate = updatedCard.ExpiryDate;
            user.Card.CvvCode = updatedCard.CvvCode;

            _context.Cards.Update(user.Card);
            await _context.SaveChangesAsync();
            return Ok(user.Card);
        }
    }
    else
    {
        // Якщо у користувача взагалі не було картки — створюємо нову
        _context.Cards.Add(updatedCard);
        await _context.SaveChangesAsync();

        user.CardId = updatedCard.Id;
        _context.Users.Update(user);
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