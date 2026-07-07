using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransportApi.Models;

namespace TransportApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReportResponseController : ControllerBase
    {
        private readonly TransportApiContext _context;

        public ReportResponseController(TransportApiContext context)
        {
            _context = context;
        }

        // GET: api/ReportResponse
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ReportResponse>>> GetReportResponses()
        {
            return await _context.ReportResponses.ToListAsync();
        }

        // GET: api/ReportResponse/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ReportResponse>> GetReportResponse(int id)
        {
            var reportResponse = await _context.ReportResponses.FindAsync(id);

            if (reportResponse == null)
            {
                return NotFound();
            }

            return reportResponse;
        }

        // POST: api/ReportResponse
        [HttpPost]
        public async Task<ActionResult<ReportResponse>> PostReportResponse(ReportResponse reportResponse)
        {
            _context.ReportResponses.Add(reportResponse);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetReportResponse", new { id = reportResponse.Id }, reportResponse);
        }
    }
}