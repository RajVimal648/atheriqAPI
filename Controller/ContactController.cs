
using atheriqAPI.Models;
using atheriqAPI.Services.Email;
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
        private readonly IEmailService _emailService;

        public ContactController(
            IConfiguration config,
            ILogger<ContactController> logger,
            IEmailService emailService)
        {
            _connectionString = config.GetConnectionString("Default")
                ?? throw new InvalidOperationException(
                    "ConnectionStrings:Default is missing.");

            _logger = logger;
            _emailService = emailService;
        }

        [HttpPost]
        [EnableRateLimiting("contact")]
        public async Task<IActionResult> Post(
            [FromBody] ContactRequest req,
            CancellationToken ct)
        {
            if (!string.IsNullOrWhiteSpace(req.Website))
                return Ok(new { success = true });

            if (!req.ConsentGiven)
            {
                ModelState.AddModelError(
                    nameof(req.ConsentGiven),
                    "Consent is required.");
            }

            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var fullName = req.FullName.Trim();
            var email = req.Email.Trim().ToLowerInvariant();
            var message = req.Message.Trim();

            const string sql = @"
                INSERT INTO dbo.ContactLeads
                    (
                        Source,
                        FullName,
                        Email,
                        Phone,
                        Company,
                        Service,
                        Subject,
                        Message,
                        ConsentGiven
                    )
                VALUES
                    (
                        @Source,
                        @FullName,
                        @Email,
                        @Phone,
                        @Company,
                        @Service,
                        @Subject,
                        @Message,
                        @ConsentGiven
                    );";

            try
            {
                await using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync(ct);

                await using var cmd = new SqlCommand(sql, conn);

                cmd.Parameters.AddWithValue("@Source", "website");
                cmd.Parameters.AddWithValue("@FullName", fullName);
                cmd.Parameters.AddWithValue("@Email", email);
                cmd.Parameters.AddWithValue(
                    "@Phone",
                    NullIfEmpty(req.Phone));
                cmd.Parameters.AddWithValue(
                    "@Company",
                    NullIfEmpty(req.Company));
                cmd.Parameters.AddWithValue(
                    "@Service",
                    NullIfEmpty(req.Service));
                cmd.Parameters.AddWithValue(
                    "@Subject",
                    "Website enquiry");
                cmd.Parameters.AddWithValue(
                    "@Message",
                    message);
                cmd.Parameters.AddWithValue(
                    "@ConsentGiven",
                    req.ConsentGiven);

                await cmd.ExecuteNonQueryAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to save contact lead.");

                return Problem(
                    "Could not save your enquiry. Please try again later.",
                    statusCode: 500);
            }

            try
            {
                await _emailService.SendLeadNotificationAsync(
                    new LeadNotification(
                        fullName,
                        email,
                        req.Phone?.Trim(),
                        req.Company?.Trim(),
                        req.Service?.Trim(),
                        message),
                    CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Lead saved, but email notification failed.");
            }

            return Ok(new { success = true });
        }

        private static object NullIfEmpty(string? value) =>
            string.IsNullOrWhiteSpace(value)
                ? DBNull.Value
                : value.Trim();
    }
}
