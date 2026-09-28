namespace atheriqAPI.Services.Email
{
    public class EmailSettings
    {
        public bool Enabled { get; set; } = true;

        // ── Brevo HTTP API (recommended for shared hosting) ──────────────────
        /// <summary>
        /// Brevo transactional API key (free at https://app.brevo.com → SMTP &amp; API → API Keys).
        /// When set, BrevoEmailService is used (HTTPS, no SMTP ports needed).
        /// </summary>
        public string BrevoApiKey { get; set; } = string.Empty;

        // ── Legacy SMTP settings (kept for fallback / local dev) ─────────────
        public string SmtpHost { get; set; } = string.Empty;

        public int SmtpPort { get; set; } = 587;

        /// <summary>
        /// Set to true for port 465 (SSL). Leave false for port 587 (StartTLS).
        /// </summary>
        public bool UseSsl { get; set; } = false;

        public string Username { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        // ── Shared ──────────────────────────────────────────────────────────
        public string FromAddress { get; set; } = string.Empty;

        public string FromName { get; set; } = "AtherIQ Website";

        public string ToAddress { get; set; } = string.Empty;
    }
}

