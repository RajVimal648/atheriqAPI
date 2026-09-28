using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace atheriqAPI.Services.Email;

public class SmtpEmailService : IEmailService
{
    private readonly EmailSettings _settings;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(
        IOptions<EmailSettings> options,
        IWebHostEnvironment environment,
        ILogger<SmtpEmailService> logger)
    {
        _settings = options.Value;
        _environment = environment;
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

        // Load both templates
        var adminTemplate = await LoadTemplateAsync(
            "EmailReceived.html",
            ct);

        var clientTemplate = await LoadTemplateAsync(
            "EmailSentClient.html",
            ct);

        // Replace placeholders
        var adminBody = BuildEmailBody(
            adminTemplate,
            lead);

        var clientBody = BuildEmailBody(
            clientTemplate,
            lead);

        // Create admin email
        var adminMessage = CreateMessage(
            to: _settings.ToAddress,
            subject: $"New website lead: {EmailHelper.Clean(lead.FullName, 80)}",
            body: adminBody,
            replyTo: lead.Email);

        // Create client email
        var clientMessage = CreateMessage(
            to: lead.Email,
            subject: "Thank you for contacting Atheriq",
            body: clientBody);

        // Choose SSL mode: UseSsl=true → port 465 (SslOnConnect), false → port 587 (StartTls)
        var socketOptions = _settings.UseSsl
            ? SecureSocketOptions.SslOnConnect
            : SecureSocketOptions.StartTls;

        _logger.LogInformation(
            "Connecting to SMTP: {Host}:{Port} (UseSsl={UseSsl})",
            _settings.SmtpHost, _settings.SmtpPort, _settings.UseSsl);

        // Send both emails using one SMTP connection
        using var client = new SmtpClient
        {
            Timeout = 30000   // 30 s — shared hosting can be slow
        };

        await client.ConnectAsync(
            _settings.SmtpHost,
            _settings.SmtpPort,
            socketOptions,
            ct);

        _logger.LogInformation("SMTP connected. Authenticating as {Username}.", _settings.Username);

        await client.AuthenticateAsync(
            _settings.Username,
            _settings.Password,
            ct);

        _logger.LogInformation("SMTP authenticated. Sending admin notification to {To}.", _settings.ToAddress);

        // Send to admin
        await client.SendAsync(
            adminMessage,
            ct);

        _logger.LogInformation("Admin email sent. Sending client confirmation to {To}.", lead.Email);

        // Send confirmation to client
        await client.SendAsync(
            clientMessage,
            ct);

        _logger.LogInformation("Client confirmation email sent. Disconnecting.");

        await client.DisconnectAsync(
            true,
            ct);
    }

    private async Task<string> LoadTemplateAsync(
        string fileName,
        CancellationToken ct)
    {
        var templatePath = Path.Combine(
            _environment.ContentRootPath,
            "Services",
            "Email",
            "EmailTemplates",
            fileName);

        if (!File.Exists(templatePath))
        {
            throw new FileNotFoundException(
                "Email template was not found.",
                templatePath);
        }

        return await File.ReadAllTextAsync(
            templatePath,
            ct);
    }

    private static string BuildEmailBody(
        string template,
        LeadNotification lead)
    {
        return template
            .Replace("{{name}}", lead.FullName ?? "-")
            .Replace("{{email}}", lead.Email ?? "-")
            .Replace("{{phone}}", lead.Phone ?? "-")
            .Replace("{{company}}", lead.Company ?? "-")
            .Replace("{{service}}", lead.Service ?? "-")
            .Replace("{{message}}", lead.Message ?? "-")
            .Replace(
                "{{time}}",
                $"{DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC");
    }

    private MimeMessage CreateMessage(
        string to,
        string subject,
        string body,
        string? replyTo = null)
    {
        var message = new MimeMessage();

        message.From.Add(
            new MailboxAddress(
                _settings.FromName,
                _settings.FromAddress));

        message.To.Add(
            MailboxAddress.Parse(to));

        if (!string.IsNullOrWhiteSpace(replyTo) &&
            MailboxAddress.TryParse(
                replyTo,
                out var replyToAddress))
        {
            message.ReplyTo.Add(replyToAddress);
        }

        message.Subject = subject;

        message.Body = new TextPart("html")
        {
            Text = body
        };

        return message;
    }
}