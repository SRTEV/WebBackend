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
    var existingCard = await _context.Cards
        .FirstOrDefaultAsync(c => c.CardNumber == card.CardNumber
            && c.ExpiryDate == card.ExpiryDate
            && c.CvvCode == card.CvvCode);

    Card cardToUse;

    if (existingCard != null)
    {
        // Якщо картка вже є в базі — використовуємо її існуючий ID
        cardToUse = existingCard;
    }
    else
    {
        _context.Cards.Add(card);
        await _context.SaveChangesAsync();
        cardToUse = card;
    }

    var user = await _context.Users.FindAsync(userId);
    if (user == null)
    {
        return NotFound("User not found");
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

    // 1. СПОЧАТКУ перевіряємо, чи є інші користувачі з цим CardId 
    // (враховуємо, що поточний теж поки має цей ID в базі)
    int usersCountWithCard = await _context.Users.CountAsync(u => u.CardId == id);

    // 2. Відв'язуємо картку від поточного користувача
    user.CardId = null;

    // 3. Якщо окрім поточного юзера цю картку більше ніхто не тримає (тобто загалом була лише 1 людина)
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

    // Перевіряємо, чи стара картка належала цьому користувачу
    // (опціонально, але корисно для безпеки, якщо 'id' в роуті — це стара карта)
    
    // 1. Шукаємо, чи нова карта вже є в базі
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

    // Запам'ятовуємо ID старої карти користувача перед зміною
    int? oldCardId = user.CardId;

    // 2. Прив'язуємо нову картку до користувача
    user.CardId = cardToUse.Id;
    await _context.SaveChangesAsync();

    // 3. Якщо у користувача була стара картка і це не та сама картка, перевіряємо чи її хтось ще використовує
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

    }
}