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
            [Authorize] // Обов'язково для отримання ID користувача
public async Task<ActionResult<Card>> PostCard(Card card)
{
    // 1. Отримуємо ID поточного користувача з токена
    var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

    // 2. Зберігаємо картку окремо, щоб отримати її новий ID
    _context.Cards.Add(card);
    await _context.SaveChangesAsync();

    // 3. Знаходимо користувача в БД
    var user = await _context.Users.FindAsync(userId);
    
    // 4. Прив'язуємо картку до користувача
    if (user != null)
    {
        user.CardId = card.Id; // Записуємо ID нової картки в поле CardId користувача
        await _context.SaveChangesAsync();
    }

    return CreatedAtAction(nameof(GetCard), new { id = card.Id }, card);
}
        
    }
}