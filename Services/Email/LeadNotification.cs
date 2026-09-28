namespace atheriqAPI.Services.Email
{
    public record LeadNotification(
        string FullName,
        string Email,
        string? Phone,
        string? Company,
        string? Service,
        string Message);
}
