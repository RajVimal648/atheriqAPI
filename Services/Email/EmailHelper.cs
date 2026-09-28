namespace atheriqAPI.Services.Email
{
    internal static class EmailHelper
    {
        // Remove line breaks to prevent header injection
        // and limit the maximum length.
        public static string Clean(string value, int max)
        {
            var cleaned = value
                .Replace("\r", " ")
                .Replace("\n", " ")
                .Trim();

            return cleaned.Length > max
                ? cleaned[..max]
                : cleaned;
        }
    }
}
