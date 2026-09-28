using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace atheriqAPI.Services.Email;

/// <summary>
/// Sends emails via Brevo (formerly Sendinblue) transactional email REST API.
/// Free tier: 300 emails/day. Uses HTTPS port 443 — works on all hosting providers.
/// Sign up at https://app.brevo.com → SMTP & API → API Keys
/// </summary>
public class BrevoEmailService : IEmailService
{
    private const string BrevoApiUrl = "https://api.brevo.com/v3/smtp/email";

    private readonly EmailSettings _settings;
    private readonly IWebHostEnvironment _environment;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<BrevoEmailService> _logger;

    public BrevoEmailService(
        IOptions<EmailSettings> options,
        IWebHostEnvironment environment,
        IHttpClientFactory httpClientFactory,
        ILogger<BrevoEmailService> logger)
    {
        _settings = options.Value;
        _environment = environment;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task SendLeadNotificationAsync(
        LeadNotification lead,
        CancellationToken ct = default)
    {
        if (!_settings.Enabled)
        {
            _logger.LogInformation("Email sending is disabled (Enabled=false). Skipping.");
            return;
        }

        if (string.IsNullOrWhiteSpace(_settings.BrevoApiKey))
        {
            _logger.LogError("Brevo API key is missing. Set EmailSettings:BrevoApiKey in appsettings.");
            return;
        }

        // Load HTML templates
        var adminHtml  = await LoadTemplateAsync("EmailReceived.html", ct);
        var clientHtml = await LoadTemplateAsync("EmailSentClient.html", ct);

        // Replace placeholders
        var adminBody  = BuildEmailBody(adminHtml,  lead);
        var clientBody = BuildEmailBody(clientHtml, lead);

        using var http = _httpClientFactory.CreateClient("brevo");

        // Send admin notification
        _logger.LogInformation("Sending admin notification to {To} via Brevo.", _settings.ToAddress);
        await SendBrevoEmailAsync(
            http,
            to:      _settings.ToAddress,
            subject: $"New website lead: {EmailHelper.Clean(lead.FullName, 80)}",
            html:    adminBody,
            replyTo: lead.Email,
            ct:      ct);

        // Send client confirmation
        _logger.LogInformation("Sending client confirmation to {To} via Brevo.", lead.Email);
        await SendBrevoEmailAsync(
            http,
            to:      lead.Email,
            subject: "Thank you for contacting Atheriq",
            html:    clientBody,
            ct:      ct);

        _logger.LogInformation("Both emails sent successfully via Brevo.");
    }

    private async Task SendBrevoEmailAsync(
        HttpClient http,
        string to,
        string subject,
        string html,
        string? replyTo = null,
        CancellationToken ct = default)
    {
        var payload = new
        {
            sender = new { name = _settings.FromName, email = _settings.FromAddress },
            to = new[] { new { email = to } },
            replyTo = replyTo is not null ? new { email = replyTo } : null,
            subject,
            htmlContent = html
        };

        var json    = JsonSerializer.Serialize(payload, new JsonSerializerOptions { DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull });
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        http.DefaultRequestHeaders.Clear();
        http.DefaultRequestHeaders.Add("api-key", _settings.BrevoApiKey);
        http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var response = await http.PostAsync(BrevoApiUrl, content, ct);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError(
                "Brevo API returned {StatusCode} for recipient {To}. Response: {Body}",
                response.StatusCode, to, body);

            response.EnsureSuccessStatusCode(); // will throw — caught by controller
        }
    }

    private async Task<string> LoadTemplateAsync(string fileName, CancellationToken ct)
    {
        var path = Path.Combine(
            _environment.ContentRootPath,
            "Services", "Email", "EmailTemplates",
            fileName);

        if (!File.Exists(path))
            throw new FileNotFoundException("Email template not found.", path);

        return await File.ReadAllTextAsync(path, ct);
    }

    private static string BuildEmailBody(string template, LeadNotification lead) =>
        template
            .Replace("{{name}}",    lead.FullName ?? "-")
            .Replace("{{email}}",   lead.Email    ?? "-")
            .Replace("{{phone}}",   lead.Phone    ?? "-")
            .Replace("{{company}}", lead.Company  ?? "-")
            .Replace("{{service}}", lead.Service  ?? "-")
            .Replace("{{message}}", lead.Message  ?? "-")
            .Replace("{{time}}",    $"{DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC");
}
