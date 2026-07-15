using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransportApi.Models;

namespace TransportApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReportController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ReportController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Report>>> GetReports()
        {
            var reports = await _context.Reports.ToListAsync();
            return Ok(reports);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Report>> GetReport(int id)
        {
            var report = await _context.Reports.FindAsync(id);

            if (report == null)
            {
                return NotFound();
            }

            return Ok(report);
        }

        [HttpPost]
        public async Task<ActionResult<Report>> PostReport(Report report)
        {
            report.CreatedAt = DateTime.UtcNow;
            
            if (string.IsNullOrEmpty(report.Status)) 
            {
                report.Status = "Pending";
            }

            if (string.IsNullOrEmpty(report.Type)) 
            {
                report.Type = "Repairman";
            }

            _context.Reports.Add(report);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetReport), new { id = report.Id }, report);
        }
    }
}