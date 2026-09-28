namespace atheriqAPI.Services.Email
{
    public class EmailSettings
    {
        public bool Enabled { get; set; } = true;

        public string SmtpHost { get; set; } = string.Empty;

        public int SmtpPort { get; set; } = 587;

        /// <summary>
        /// Set to true for port 465 (SSL). Leave false for port 587 (StartTLS).
        /// MonsterASP may require true if port 587 is blocked.
        /// </summary>
        public bool UseSsl { get; set; } = false;

        public string Username { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        public string FromAddress { get; set; } = string.Empty;

        public string FromName { get; set; } = "AtherIQ Website";

        public string ToAddress { get; set; } = string.Empty;
    }
}
