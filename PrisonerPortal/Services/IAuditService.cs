namespace PrisonerPortal.Services
{
    public interface IAuditService
    {
        Task LogAsync(string? userId, string action, string entityType, string entityId, string? oldValues = null, string? newValues = null);
    }
}
