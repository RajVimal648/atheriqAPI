using atheriqAPI.Services.Email;
using Microsoft.AspNetCore.Mvc;

namespace atheriqAPI.Controller
{
    /// <summary>
    /// Temporary diagnostic endpoint — remove before final production release.
    /// Surfaces the real SMTP error so we can see what's blocking email on MonsterASP.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class EmailTestController : ControllerBase
    {
        private readonly IEmailService _emailService;

        public EmailTestController(IEmailService emailService)
        {
            _emailService = emailService;
        }

        [HttpGet]
        public async Task<IActionResult> Test(CancellationToken ct)
        {
            try
            {
                await _emailService.SendLeadNotificationAsync(
                    new LeadNotification(
                        FullName: "Production Diagnostic",
                        Email: "rajvimal648@gmail.com",
                        Phone: null,
                        Company: null,
                        Service: null,
                        Message: "SMTP diagnostic test from production server."),
                    ct);

                return Ok(new { success = true, message = "Both emails sent successfully via SMTP." });
            }
            catch (Exception ex)
            {
                // Intentionally expose full error for diagnosis
                return StatusCode(500, new
                {
                    success = false,
                    error = ex.GetType().Name,
                    message = ex.Message,
                    innerError = ex.InnerException?.Message
                });
            }
        }
    }
}
