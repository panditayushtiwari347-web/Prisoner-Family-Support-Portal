using PrisonerPortal.Models.Entities;

namespace PrisonerPortal.Services
{
    public interface ISupportRequestService
    {
        Task<SupportRequest> SubmitRequestAsync(string userId, int prisonerId, string category, string subject, string description, string priority);
        Task UpdateStatusAsync(int requestId, string status, string adminId);
        Task AddAdminResponseAsync(int requestId, string response, string adminId);
        Task AssignAdminAsync(int requestId, string adminId);
        Task<IEnumerable<SupportRequest>> GetUserRequestsAsync(string userId);
        Task<IEnumerable<SupportRequest>> GetAllRequestsAsync();
        Task<SupportRequest?> GetByIdAsync(int id);
    }
}
