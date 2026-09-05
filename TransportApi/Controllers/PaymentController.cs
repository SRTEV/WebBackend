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

       [HttpPost("pay/{rentalId}/{amount}/{userId}")]
public async Task<IActionResult> Pay(int rentalId, decimal amount, int userId, [FromQuery] string paymentMethodId = "pm_card_visa")
{
    using var transaction = await _context.Database.BeginTransactionAsync();
    try
    {
        var rental = await _context.Rentals.FindAsync(rentalId);
        if (rental == null) return NotFound(new { message = "Rental not found." });

        var user = await _context.Users.FindAsync(userId);
        if (user == null) return NotFound(new { message = "User not found." });

        decimal originalAmount = amount;

        // 1. Шукаємо активну нагороду користувача (де RewardAmount > 0)
        var userResult = await _context.UsersResults
            .Where(ur => ur.UserId == userId && ur.RewardAmount > 0)
            .FirstOrDefaultAsync();

        if (userResult != null && userResult.Rank.HasValue)
        {
            // Отримуємо всі нагороди для цього змагання, відсортовані за ID (або по порядку місць)
            var rewards = await _context.RewardTypes
                .Where(rt => rt.CompetitionId == userResult.CompetitionId)
                .OrderBy(rt => rt.Id)
                .ToListAsync();

            RewardType? matchedReward = null;
            int rank = userResult.Rank.Value;

            // Визначаємо нагороду відповідно до рангу (як у вашому іншому ендпоінті)
            if (rank == 1)
            {
                matchedReward = rewards.ElementAtOrDefault(0);
            }
            else if (rank >= 2 && rank <= 4)
            {
                matchedReward = rewards.ElementAtOrDefault(1);
            }
            else if (rank >= 5)
            {
                matchedReward = rewards.ElementAtOrDefault(2);
            }

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

                // Явно повідомляємо EF про зміни в нагороді
                _context.UsersResults.Update(userResult);
            }
        }

        string paymentStatus = "Completed";

        // 2. Обробка платежу через Stripe, якщо залишилась сума до сплати
        if (amount > 0)
        {
            try
            {
                long amountInCents = (long)(amount * 100);

                var options = new PaymentIntentCreateOptions
                {
                    Amount = amountInCents,
                    Currency = "pln",
                    PaymentMethod = string.IsNullOrEmpty(paymentMethodId) ? "pm_card_visa" : paymentMethodId,
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
                await transaction.RollbackAsync();
                return BadRequest(new { 
                    message = "Stripe error occurred", 
                    stripeError = ex.StripeError?.Message ?? ex.Message,
                    errorCode = ex.StripeError?.Code
                });
            }
        }
        else
        {
            paymentStatus = "Completed via Reward";
        }

        // 3. Створюємо запис про платіж
        var payment = new Payment
        {
            RentalId = rentalId,
            Amount = amount,
            Status = paymentStatus,
            CreatedAt = DateTime.UtcNow
        };

        _context.Payments.Add(payment);
        await _context.SaveChangesAsync(); // Зберігаємо платіж і отримуємо payment.Id

        // 4. Зв'язуємо нагороду зі створеним платежем
        if (userResult != null)
        {
            userResult.PaymentId = payment.Id;
            _context.UsersResults.Update(userResult);
        }

        // 5. Завершення транзакції залежно від статусу платежу
        if (paymentStatus == "Failed")
        {
            user.OustandingBalances += originalAmount;
            _context.Users.Update(user);
            
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            
            return BadRequest(new { message = "Payment failed or card declined. Added to outstanding balance.", paymentId = payment.Id });
        }
        else if (paymentStatus == "Processing")
        {
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            
            return Accepted(new { message = "Payment requires further action.", paymentId = payment.Id });
        }

        // Успішне збереження змін та коміт транзакції
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return Ok(new { message = "Payment processed successfully.", finalAmount = amount, paymentId = payment.Id });
    }
    catch (Exception)
    {
        await transaction.RollbackAsync();
        throw;
    }
}
    }
}