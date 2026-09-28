namespace atheriqAPI.Services.Email
{
    public interface IEmailService
    {
        Task SendLeadNotificationAsync(
            LeadNotification lead,
            CancellationToken ct = default);
    }
}
