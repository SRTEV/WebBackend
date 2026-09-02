using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransportApi.Models;
using Microsoft.AspNetCore.Authorization;

namespace TransportApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RentalController : ControllerBase
    {
        private readonly AppDbContext _context;
        public RentalController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Rental
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Rental>>> GetRentals()
        {
            var rentals = await _context.Rentals
                .Include(r => r.User) 
                .Include(r => r.Vehicle) 
                .ToListAsync();

            return Ok(rentals);
        }

        // GET: api/Rental/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Rental>> GetRental(int id)
        {
            var rental = await _context.Rentals.FindAsync(id);

            if (rental == null)
            {
                return NotFound();
            }

            return Ok(rental);
        }

        // POST: api/Rental
        [HttpPost]
        public async Task<ActionResult<Rental>> PostRental(Rental rental)
        {
            _context.Rentals.Add(rental);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetRental), new { id = rental.Id }, rental);
        }

        [HttpPost("start")]
        [Authorize]
        public async Task<ActionResult<Rental>> StartRental([FromBody] StartRentalDto dto)
        {
            var vehicle = await _context.Vehicles
                .Include(v => v.VehicleStatus)
                .FirstOrDefaultAsync(v => v.Id == dto.VehicleId);

            if (vehicle == null)
            {
                return NotFound(new { message = "Vehicle not found." });
            }

            if (vehicle.VehicleStatus?.Name == "Rented")
            {
                return BadRequest(new { message = "This vehicle is already in active rental." });
            }
            else if (vehicle.VehicleStatus?.Name != "Available")
            {
                return BadRequest(new { message = $"Vehicle is unavailable" });
            }
         
            var rentedStatus = await _context.VehicleStatuses
                .FirstOrDefaultAsync(s => s.Name == "Rented");

            if (rentedStatus == null)
            {
                return StatusCode(500, new { message = "Status 'Rented' is not found in the database." });
            }

            var rental = new Rental
            {
                VehicleId = dto.VehicleId,
                RentalPlanId = dto.RentalPlanId,
                UserId = dto.UserId,
                StartTime = DateTime.UtcNow
            };

            _context.Rentals.Add(rental);

            vehicle.VehicleStatusId = rentedStatus.Id;
            vehicle.ScanTime = DateTime.UtcNow;
            _context.Vehicles.Update(vehicle);
            
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetRental), new { id = rental.Id }, rental);
        }

        // POST: api/Rental/end
        [HttpPost("end")]
        [Authorize]
        public async Task<ActionResult<Rental>> EndRental([FromBody] EndRentalDto dto)
        {
            var rental = await _context.Rentals
                .Include(r => r.Vehicle)
                .FirstOrDefaultAsync(r => r.Id == dto.RentalId);

            if (rental == null)
            {
                return NotFound(new { message = "Rental not found." });
            }

            if (rental.EndTime != null)
            {
                return BadRequest(new { message = "This rental has already been ended." });
            }

            var availableStatus = await _context.VehicleStatuses
                .FirstOrDefaultAsync(s => s.Name == "Available");

            if (availableStatus == null)
            {
                return StatusCode(500, new { message = "Status 'Available' is not found in the database." });
            }
           
            rental.EndTime = DateTime.UtcNow;
            rental.Distance = (decimal)dto.Distance; 
            
            _context.Rentals.Update(rental);
            
            rental.Vehicle.VehicleStatusId = availableStatus.Id;
            _context.Vehicles.Update(rental.Vehicle);

    
            var currentDate = DateOnly.FromDateTime(DateTime.UtcNow);
            var vehicleTypeId = rental.Vehicle.VehicleTypeId; 
            var userId = rental.UserId;

            // Конвертуємо кілометри у метри для цілочисельного Score
            int distanceScore = (int)Math.Round(dto.Distance * 1000);

            var activeCompetitions = await _context.Competitions
                .Where(c => c.VehicleTypeId == vehicleTypeId && c.StartDate <= currentDate && c.EndDate >= currentDate)
                .Include(c => c.GoalTypes)
                .ToListAsync();

            foreach (var competition in activeCompetitions)
            {
                bool isMarathon = competition.GoalTypes.Any(gt => gt.Name == "Marathon");
                if (!isMarathon) continue;

                var userResult = await _context.UsersResults
                    .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.CompetitionId == competition.Id);

                if (userResult != null)
                {
                    userResult.Score += distanceScore;
                    _context.UsersResults.Update(userResult);
                }
                else
                {
                    var newUserResult = new UsersResult
                    {
                        UserId = userId,
                        CompetitionId = competition.Id,
                        Score = distanceScore,
                        Rank = 0,
                        RewardAmount = 0
                    };
                    _context.UsersResults.Add(newUserResult);
                }
            }
            // ==========================================

            await _context.SaveChangesAsync();

            return Ok(rental);
        }

        // GET: api/Rental/History/{userId}
        [HttpGet("History/{userId}")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<Rental>>> GetRentalHistory(int userId)
        {
            var rentals = await _context.Rentals
                .Include(r => r.Vehicle)
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.StartTime)
                .AsNoTracking()
                .ToListAsync();

            if (rentals == null || rentals.Count == 0)
            {
                return NotFound(new { message = "No rental history found for this user." });
            }

            return Ok(rentals);
        }

        public class StartRentalDto
        {
            public int VehicleId { get; set; }
            public int RentalPlanId { get; set; }
            public int UserId { get; set; }
        }

        public class EndRentalDto
        {
            public int RentalId { get; set; }
            public double Distance { get; set; }
            public decimal positionX { get; set; }
            public decimal positionY { get; set; }
        }
    }
}