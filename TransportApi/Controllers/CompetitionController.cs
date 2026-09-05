using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransportApi.Models;
using Microsoft.AspNetCore.Authorization;   
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
[HttpGet("latest/{vehicleTypeId}")]
[Authorize]
        public async Task<IActionResult> GetLatestCompetition(int vehicleTypeId)
        {
            var currentDate = DateOnly.FromDateTime(DateTime.UtcNow);
            var competition = await _context.Competitions
                .Include(c => c.VehicleType)
                .Include(c => c.GoalTypes)
                .Include(c => c.RewardTypes)
                .Where(c => c.VehicleTypeId == vehicleTypeId && c.StartDate <= currentDate && c.EndDate >= currentDate)
                .OrderByDescending(c => c.StartDate)
                .FirstOrDefaultAsync();

            bool isEnded = false;

            if (competition == null)
            {
                competition = await _context.Competitions
                    .Include(c => c.VehicleType)
                    .Include(c => c.GoalTypes)
                    .Include(c => c.RewardTypes)
                    .Where(c => c.VehicleTypeId == vehicleTypeId && c.EndDate < currentDate)
                    .OrderByDescending(c => c.EndDate)
                    .FirstOrDefaultAsync();

                if (competition != null)
                {
                    isEnded = true; 
                }
            }

    
            if (competition == null)
            {
                return NotFound(new { message = "Challenge not found" });
            }
            return Ok(new
            {
                competition.Id,
                StartDate = competition.StartDate.ToString("yyyy-MM-dd"),
                EndDate = competition.EndDate.ToString("yyyy-MM-dd"),
                Description = competition.Description ?? "",
                competition.GoalValue,
                competition.VehicleTypeId,
                VehicleTypeName = competition.VehicleType?.Name ?? "",
                IsEnded = isEnded, // true — якщо завершився, false — якщо активний
                GoalTypes = competition.GoalTypes.Select(g => new { g.Id, Name = g.Name ?? "" }),
                RewardTypes = competition.RewardTypes.Select(r => new { r.Id, Name = r.Name ?? "", Unit = r.Unit ?? "" })
            });
        }

[HttpPost]
public async Task<IActionResult> PostCompetition([FromBody] Competition competition)
{
    if (!ModelState.IsValid)
    {
        return BadRequest(ModelState);
    }

    competition.VehicleType = null!;

    var incomingGoals = competition.GoalTypes.ToList();
    var incomingRewards = competition.RewardTypes.ToList();
    
    competition.GoalTypes.Clear();
    competition.RewardTypes.Clear();

    foreach (var g in incomingGoals)
    {
        var existingGoal = await _context.GoalTypes.FindAsync(g.Id);
        if (existingGoal != null)
        {
            competition.GoalTypes.Add(existingGoal);
        }
    }

    foreach (var r in incomingRewards)
    {
        var existingReward = await _context.RewardTypes.FindAsync(r.Id);
        if (existingReward != null)
        {
            competition.RewardTypes.Add(existingReward);
        }
    }

    _context.Competitions.Add(competition);
    await _context.SaveChangesAsync();

    return CreatedAtAction(nameof(GetCompetition), new { id = competition.Id }, competition);
}

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

            existingComp.StartDate = competition.StartDate;
            existingComp.EndDate = competition.EndDate;
            existingComp.Description = competition.Description;
            existingComp.GoalValue = competition.GoalValue;
            existingComp.VehicleTypeId = competition.VehicleTypeId;

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

    [HttpGet("UserResults/{userId}")]
    [Authorize]
    public async Task<IActionResult> GetAllUserResults(int userId)
    {
        var userResults = await _context.UsersResults
            .Where(ur => ur.UserId == userId)
            .Include(ur => ur.Competition)
                .ThenInclude(c => c.VehicleType)
            .Include(ur => ur.Competition)
                .ThenInclude(c => c.RewardTypes)
            .AsNoTracking()
            .ToListAsync();

        if (userResults == null || !userResults.Any())
        {
            return NotFound(new { message = "No results found for this user." });
        }

        var responseList = userResults.Select(userResult =>
        {
            var rewards = userResult.Competition?.RewardTypes?.OrderBy(rt => rt.Id).ToList() ?? new List<RewardType>();

            RewardType? matchedReward = null;
            int rank = userResult.Rank.Value;

            if (rank == 1)
            {
                matchedReward = rewards.ElementAtOrDefault(0);
            }
            else if (rank >= 2 && rank <= 4)
            {
                matchedReward = rewards.ElementAtOrDefault(1);
            }
            else if (rank == 5)
            {
                matchedReward = rewards.ElementAtOrDefault(2);
            }

            return new
            {
                userResult.Id,
                userResult.UserId,
                userResult.CompetitionId,
                userResult.Score,
                userResult.Rank,
                userResult.RewardAmount,
                rewardName = matchedReward?.Name ?? "No reward",
                rewardUnit = matchedReward?.Unit ?? "0",
                challengeTypeName = userResult.Competition?.Description ?? "Challenge",
                vehicleTypeName = userResult.Competition?.VehicleType?.Name ?? "",
                startDate = userResult.Competition?.StartDate.ToString("yyyy-MM-dd"),
                endDate = userResult.Competition?.EndDate.ToString("yyyy-MM-dd")
            };
        }).ToList();

        return Ok(responseList);
    }
        

        
        }
    }

