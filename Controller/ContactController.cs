using atheriqAPI.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Data.SqlClient;

namespace atheriqAPI.Controller
{

    [ApiController]
    [Route("api/[controller]")]
    public class ContactController : ControllerBase
    {
        private readonly string _connectionString;
        private readonly ILogger<ContactController> _logger;

        public ContactController(IConfiguration config, ILogger<ContactController> logger)
        {
            _connectionString = config.GetConnectionString("Default")
                ?? throw new InvalidOperationException("ConnectionStrings:Default is missing.");
            _logger = logger;
        }

        // POST api/contact
        [HttpPost]
        [EnableRateLimiting("contact")]
        public async Task<IActionResult> Post([FromBody] ContactRequest req, CancellationToken ct)
        {
            // Honeypot filled = bot. Pretend success, store nothing.
            if (!string.IsNullOrWhiteSpace(req.Website))
                return Ok(new { success = true });

            if (!req.ConsentGiven)
                ModelState.AddModelError(nameof(req.ConsentGiven), "Consent is required.");

            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            const string sql = @"
            INSERT INTO dbo.ContactLeads
                (Source, FullName, Email, Phone, Company, Service, Subject, Message, ConsentGiven)
            VALUES
                (@Source, @FullName, @Email, @Phone, @Company, @Service, @Subject, @Message, @ConsentGiven);";

            try
            {
                await using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync(ct);

                await using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@Source", "website");
                cmd.Parameters.AddWithValue("@FullName", req.FullName.Trim());
                cmd.Parameters.AddWithValue("@Email", req.Email.Trim().ToLowerInvariant());
                cmd.Parameters.AddWithValue("@Phone", NullIfEmpty(req.Phone));
                cmd.Parameters.AddWithValue("@Company", NullIfEmpty(req.Company));
                cmd.Parameters.AddWithValue("@Service", NullIfEmpty(req.Service));
                cmd.Parameters.AddWithValue("@Subject", "Website enquiry");
                cmd.Parameters.AddWithValue("@Message", req.Message.Trim());
                cmd.Parameters.AddWithValue("@ConsentGiven", req.ConsentGiven);

                await cmd.ExecuteNonQueryAsync(ct);
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save contact lead.");
                return Problem("Could not save your enquiry. Please try again later.", statusCode: 500);
            }
        }

        private static object NullIfEmpty(string? value) =>
            string.IsNullOrWhiteSpace(value) ? DBNull.Value : value.Trim();
    }
}
