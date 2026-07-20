using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransportApi.Models;
using TransportApi.Services;

namespace TransportApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReportController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IEmailService _emailService;

        public ReportController(AppDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }
        public class ReplyRequest
        {
            public string Email { get; set; } = string.Empty;
            public string Message { get; set; } = string.Empty;
        }

        // POST: api/Report/{id}/reply
        [HttpPost("{id}/reply")]
        public async Task<IActionResult> ReplyToReport(int id, [FromBody] ReplyRequest request)
        {
            if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Message))
            {
                return BadRequest("Email and Message are required.");
            }

            var report = await _context.Reports.FindAsync(id);
            if (report == null)
            {
                return NotFound($"Report with ID {id} not found.");
            }

            try
            {
                string subject = $"Response to your Report #{id}";
                
                await _emailService.SendReportReplyEmail(request.Email, subject, request.Message);

                report.Status = "Answered"; 
                await _context.SaveChangesAsync();

                return Ok(new { message = "Reply sent successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // GET: api/Report
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Report>>> GetReports()
        {
            var reports = await _context.Reports.ToListAsync();
            return Ok(reports);
        }

        // GET: api/Report/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Report>> GetReport(int id)
        {
            var report = await _context.Reports.FindAsync(id);
            if (report == null) return NotFound();
            return Ok(report);
        }

        // POST: api/Report
        [HttpPost]
        public async Task<ActionResult<Report>> PostReport(Report report)
        {
            _context.Reports.Add(report);
            report.CreatedAt = DateTime.Now;
            report.Status = "NotReviewed";
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetReport), new { id = report.Id }, report);
        }
    }
}