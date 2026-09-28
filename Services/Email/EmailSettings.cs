namespace atheriqAPI.Services.Email
{
    public class EmailSettings
    {
        public bool Enabled { get; set; } = true;

        public string SmtpHost { get; set; } = string.Empty;

        public int SmtpPort { get; set; } = 587;

        public string Username { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        public string FromAddress { get; set; } = string.Empty;

        public string FromName { get; set; } = "AtherIQ Website";

        public string ToAddress { get; set; } = string.Empty;
    }
}
