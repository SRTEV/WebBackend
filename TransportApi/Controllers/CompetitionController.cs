using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransportApi.Models;

namespace TransportApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CompetitionController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CompetitionController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Competition
        [HttpGet]
        public async Task<IActionResult> GetCompetitions()
        {
            try
            {
                var competitions = await _context.Competitions
                    .Include(c => c.VehicleType)
                    .Include(c => c.GoalTypes)
                    .Include(c => c.RewardTypes)
                    .Select(c => new
                    {
                        c.Id,
                        StartDate = c.StartDate.ToString("yyyy-MM-dd"),
                        EndDate = c.EndDate.ToString("yyyy-MM-dd"),
                        Description = c.Description ?? "",
                        c.GoalValue,
                        c.VehicleTypeId,
                        VehicleTypeName = c.VehicleType != null ? c.VehicleType.Name : "",
                        GoalTypes = c.GoalTypes.Select(g => new { g.Id, Name = g.Name ?? "" }),
                        RewardTypes = c.RewardTypes.Select(r => new { r.Id, Name = r.Name ?? "", Unit = r.Unit ?? "" })
                    })
                    .ToListAsync();

                return Ok(competitions);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        // GET: api/Competition/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetCompetition(int id)
        {
            var c = await _context.Competitions
                .Include(c => c.VehicleType)
                .Include(c => c.GoalTypes)
                .Include(c => c.RewardTypes)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (c == null) return NotFound();

            return Ok(new
            {
                c.Id,
                StartDate = c.StartDate.ToString("yyyy-MM-dd"),
                EndDate = c.EndDate.ToString("yyyy-MM-dd"),
                Description = c.Description ?? "",
                c.GoalValue,
                c.VehicleTypeId,
                VehicleTypeName = c.VehicleType?.Name ?? "",
                GoalTypes = c.GoalTypes.Select(g => new { g.Id, Name = g.Name ?? "" }),
                RewardTypes = c.RewardTypes.Select(r => new { r.Id, Name = r.Name ?? "", Unit = r.Unit ?? "" })
            });
        }

        // POST: api/Competition
        [HttpPost]
        public async Task<IActionResult> PostCompetition([FromBody] Competition competition)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Очищаємо навігаційне поле VehicleType, щоб EF не намагався створити новий тип транспорту
            competition.VehicleType = null!;

            _context.Competitions.Add(competition);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetCompetition), new { id = competition.Id }, competition);
        }

        // PUT: api/Competition/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutCompetition(int id, [FromBody] Competition competition)
        {
            if (id != competition.Id)
            {
                return BadRequest("ID mismatch");
            }

            var existingComp = await _context.Competitions
                .Include(c => c.GoalTypes)
                .Include(c => c.RewardTypes)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (existingComp == null)
            {
                return NotFound();
            }

            // Оновлюємо основні поля челенджу
            existingComp.StartDate = competition.StartDate;
            existingComp.EndDate = competition.EndDate;
            existingComp.Description = competition.Description;
            existingComp.GoalValue = competition.GoalValue;
            existingComp.VehicleTypeId = competition.VehicleTypeId;

            // Перезаписуємо GoalTypes та RewardTypes
            _context.GoalTypes.RemoveRange(existingComp.GoalTypes);
            _context.RewardTypes.RemoveRange(existingComp.RewardTypes);

            if (competition.GoalTypes != null && competition.GoalTypes.Any())
            {
                foreach (var gt in competition.GoalTypes)
                {
                    existingComp.GoalTypes.Add(new GoalType
                    {
                        Name = gt.Name,
                        CompetitionId = id
                    });
                }
            }

            if (competition.RewardTypes != null && competition.RewardTypes.Any())
            {
                foreach (var rt in competition.RewardTypes)
                {
                    existingComp.RewardTypes.Add(new RewardType
                    {
                        Name = rt.Name,
                        Unit = rt.Unit,
                        CompetitionId = id
                    });
                }
            }

            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}