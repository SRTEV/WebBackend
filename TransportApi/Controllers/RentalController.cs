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
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new { message = "Invalid token user ID." });
            }

            // Перевірка боргу користувача
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound(new { message = "User not found." });
            }

            if (user.OustandingBalances > 10)
            {
                return BadRequest(new { message = "Rental denied. Your outstanding balance exceeds 10 PLN." });
            }
            if(user.CardId == null)
            {
                return BadRequest(new { message = "Rental denied. You need to add a payment card before starting a rental." });
            }

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

            // Шукаємо останню завершену оренду для цього самоката
            var lastRental = await _context.Rentals
                .Where(r => r.VehicleId == dto.VehicleId && r.EndTime != null)
                .OrderByDescending(r => r.EndTime)
                .AsNoTracking()
                .FirstOrDefaultAsync();

            int lastDistanceEnd = lastRental?.DistanceEnd ?? 0;

            // Створюємо новий об'єкт оренди
            var rental = new Rental
            {
                VehicleId = dto.VehicleId,
                RentalPlanId = dto.RentalPlanId,
                UserId = userId,
                StartTime = DateTime.UtcNow,
                DistanceStart = lastDistanceEnd
            };

            _context.Rentals.Add(rental);

            // Оновлюємо статус самоката напряму
            vehicle.VehicleStatusId = rentedStatus.Id;
            vehicle.ScanTime = DateTime.UtcNow;
            _context.Entry(vehicle).State = EntityState.Modified;

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

            // 1. Отримуємо всі точки маршруту, які самокат записав у Route_History для цієї оренди
            var routePoints = await _context.RouteHistories
                .Where(rh => rh.RentalId == rental.Id)
                .OrderBy(rh => rh.RecordedAt)
                .ToListAsync();

            // 2. Рахуємо загальну дистанцію по сегментах у кілометрах
            double totalDistanceKm = 0.0;
            for (int i = 0; i < routePoints.Count - 1; i++)
            {
                var p1 = routePoints[i];
                var p2 = routePoints[i + 1];
                
                totalDistanceKm += CalculateDistance((double)p1.PositionX, (double)p1.PositionY, (double)p2.PositionX, (double)p2.PositionY);
            }

            // 3. Конвертуємо кілометри у цілі метри (int)
            int distanceMeters = (int)Math.Round(totalDistanceKm * 1000);

            int distanceStart = rental.DistanceStart; 
            int distanceEnd = distanceStart + distanceMeters;

            // 4. Записуємо дані в оренду
            rental.EndTime = DateTime.UtcNow;
            rental.Distance = distanceMeters;
            rental.DistanceStart = distanceStart;
            rental.DistanceEnd = distanceEnd; 
            
            _context.Rentals.Update(rental);
            
            rental.Vehicle.VehicleStatusId = availableStatus.Id;
            _context.Vehicles.Update(rental.Vehicle);

            // 5. Розрахунок балів для марафонів та експедицій
            var currentDate = DateOnly.FromDateTime(DateTime.UtcNow);
            var vehicleTypeId = rental.Vehicle.VehicleTypeId; 
            var userId = rental.UserId;

            int distanceScore = distanceMeters; // Бали дорівнюють метрам

            var activeCompetitions = await _context.Competitions
                .Where(c => c.VehicleTypeId == vehicleTypeId && c.StartDate <= currentDate && c.EndDate >= currentDate)
                .Include(c => c.GoalTypes)
                .ToListAsync();

            foreach (var competition in activeCompetitions)
            {
                bool isMarathon = competition.GoalTypes.Any(gt => gt.Name == "Marathon");
                bool isExpedition = competition.GoalTypes.Any(gt => gt.Name == "Expedition");

                if (!isMarathon && !isExpedition) continue;

                var userResult = await _context.UsersResults
                    .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.CompetitionId == competition.Id);

                if (isMarathon)
                {
                    // Логіка марафону: сумуємо дистанції
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
                else if (isExpedition)
                {
                    // Логіка Expedition: беремо найбільшу відстань за один проїзд (не сумуємо)
                    if (userResult != null)
                    {
                        if (distanceScore > userResult.Score)
                        {
                            userResult.Score = distanceScore;
                            _context.UsersResults.Update(userResult);
                        }
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
            }

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

        private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return 6371.0 * c; 
        }

        private double ToRadians(double angle) => angle * Math.PI / 180.0;

        public class StartRentalDto
        {
            public int VehicleId { get; set; }
            public int RentalPlanId { get; set; }
            public int UserId { get; set; }
        }

        public class EndRentalDto
        {
            public int RentalId { get; set; }
        }
    }
}