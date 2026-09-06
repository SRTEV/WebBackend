using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stripe;
using TransportApi.Models;

namespace TransportApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public PaymentController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
            
            StripeConfiguration.ApiKey = _configuration["STRIPE"];
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Payment>>> GetPayments()
        {
            return Ok(await _context.Payments.ToListAsync());
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Payment>> GetPayment(int id)
        {
            var payment = await _context.Payments.FindAsync(id);
            if (payment == null) return NotFound();
            return Ok(payment);
        }

        [HttpPost]
        public async Task<ActionResult<Payment>> PostPayment(Payment payment)
        {
            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetPayment), new { id = payment.Id }, payment);
        }

        [HttpPost("pay/{rentalId}/{userId}")]
        public async Task<IActionResult> Pay(int rentalId, int userId, [FromQuery] string? paymentMethodId = null)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Отримуємо оренду разом із планом
                var rental = await _context.Rentals
                    .Include(r => r.RentalPlan) 
                    .FirstOrDefaultAsync(r => r.Id == rentalId);

                if (rental == null) return NotFound(new { message = "Rental not found." });

                var user = await _context.Users
                    .Include(u => u.Card)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null) return NotFound(new { message = "User not found." });

                // Беремо Stripe ID напряму з параметра (свіжий pm_...) або з бази як фоллбек
                string stripePaymentMethodId = paymentMethodId;
                if (string.IsNullOrEmpty(stripePaymentMethodId))
                {
                    stripePaymentMethodId = user.Card?.CvvCode ?? "pm_card_visa";
                }

                // 2. Розрахунок суми на основі тарифу та часу
                DateTime startTime = rental.StartTime;
                DateTime endTime = DateTime.UtcNow;
                
                decimal totalMinutes = (decimal)(endTime - startTime).TotalMinutes;
                if (totalMinutes < 1) totalMinutes = 1;

                decimal planPrice = rental.RentalPlan?.Price ?? 10.0m; 
                decimal planTimeMinutes = rental.RentalPlan?.Time ?? 30.0m;

                decimal pricePerMinute = planPrice / planTimeMinutes;
                decimal amount = Math.Round(pricePerMinute * totalMinutes, 2);
                decimal originalAmount = amount;

                // 3. Шукаємо активну нагороду користувача і віднімаємо її
                var userResult = await _context.UsersResults
                    .Where(ur => ur.UserId == userId && ur.RewardAmount > 0)
                    .FirstOrDefaultAsync();

                if (userResult != null && userResult.Rank.HasValue)
                {
                    var rewards = await _context.RewardTypes
                        .Where(rt => rt.CompetitionId == userResult.CompetitionId)
                        .OrderBy(rt => rt.Id)
                        .ToListAsync();

                    RewardType? matchedReward = null;
                    int rank = userResult.Rank.Value;

                    if (rank == 1) matchedReward = rewards.ElementAtOrDefault(0);
                    else if (rank >= 2 && rank <= 4) matchedReward = rewards.ElementAtOrDefault(1);
                    else if (rank >= 5) matchedReward = rewards.ElementAtOrDefault(2);

                    if (matchedReward != null)
                    {
                        string rewardName = matchedReward.Name?.ToLower().Trim() ?? string.Empty;

                        if (rewardName.Contains("free km"))
                        {
                            decimal freeKmAvailable = userResult.RewardAmount;
                            if (amount <= freeKmAvailable)
                            {
                                userResult.RewardAmount -= (int)amount;
                                amount = 0;
                            }
                            else
                            {
                                amount -= freeKmAvailable;
                                userResult.RewardAmount = 0;
                            }
                        }
                        else if (rewardName.Contains("discount"))
                        {
                            decimal discountPercent = userResult.RewardAmount;
                            amount = amount - (amount * discountPercent / 100);
                            userResult.RewardAmount = 0;
                        }
                        else if (rewardName.Contains("free ride"))
                        {
                            if (int.TryParse(matchedReward.Unit, out int rewardPlanId) && rental.RentalPlanId == rewardPlanId)
                            {
                                decimal tariffCoveredAmount = userResult.RewardAmount; 
                                amount = amount <= tariffCoveredAmount ? 0 : amount - tariffCoveredAmount;
                                userResult.RewardAmount = 0;
                            }
                        }

                        _context.UsersResults.Update(userResult);
                    }
                }

                // Захист від занадто малих сум (мінімум 2.00 PLN за вимогами Stripe)
                if (amount > 0)
                {
                    amount = Math.Max(2.00m, Math.Round(amount, 2));
                }
                
                string paymentStatus = "Completed";

                // 4. Обробка платежу через Stripe
                if (amount > 0)
                {
                    try
                    {
                        long amountInCents = (long)(amount * 100);

                        var options = new PaymentIntentCreateOptions
                        {
                            Amount = amountInCents,
                            Currency = "pln",
                            PaymentMethod = stripePaymentMethodId, 
                            Confirm = true,
                            AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
                            {
                                Enabled = true,
                                AllowRedirects = "never"
                            }
                        };

                        var service = new PaymentIntentService();
                        var paymentIntent = await service.CreateAsync(options);

                        if (paymentIntent.Status == "succeeded")
                        {
                            paymentStatus = "Completed";
                        }
                        else if (paymentIntent.Status == "requires_action" || paymentIntent.Status == "processing")
                        {
                            paymentStatus = "Processing";
                        }
                        else
                        {
                            paymentStatus = "Failed";
                        }
                    }
                    catch (StripeException ex)
                    {
                        paymentStatus = "Failed";
                        Console.WriteLine($"=== STRIPE ERROR ===");
                        Console.WriteLine($"Message: {ex.Message}");
                        Console.WriteLine($"Stripe Error Code: {ex.StripeError?.Code}");
                        Console.WriteLine($"Decline Code: {ex.StripeError?.DeclineCode}");
                        Console.WriteLine($"Parameter: {ex.StripeError?.Param}");
                    }
                }
                else
                {
                    paymentStatus = "Completed via Reward";
                }

                // 5. Створюємо запис про платіж
                var payment = new Payment
                {
                    RentalId = rentalId,
                    Amount = amount,
                    Status = paymentStatus,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Payments.Add(payment);
                await _context.SaveChangesAsync(); 

                if (userResult != null)
                {
                    userResult.PaymentId = payment.Id;
                    _context.UsersResults.Update(userResult);
                }

                // 6. Фіксація статусу та можливого боргу
                if (paymentStatus == "Failed")
                {
                    user.OustandingBalances += originalAmount;
                    _context.Users.Update(user);
                    
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    
                    return BadRequest(new { message = "Payment failed or card declined. Added to outstanding balance.", paymentId = payment.Id, calculatedAmount = originalAmount });
                }
                else if (paymentStatus == "Processing")
                {
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    
                    return Accepted(new { message = "Payment requires further action.", paymentId = payment.Id, calculatedAmount = amount });
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { message = $" Payment processed successfully. Final amount: {amount} PLN.", finalAmount = amount, paymentId = payment.Id });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { message = "Internal server error during payment.", error = ex.Message });
            }
        }
    }
}