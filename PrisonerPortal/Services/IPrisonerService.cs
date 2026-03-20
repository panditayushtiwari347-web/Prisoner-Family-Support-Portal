using PrisonerPortal.Models.Entities;

namespace PrisonerPortal.Services
{
    public interface IPrisonerService
    {
        Task<IEnumerable<Prisoner>> GetAllPrisonersAsync();
        Task<Prisoner?> GetByIdAsync(int id);
        Task<IEnumerable<Prisoner>> SearchPrisonersAsync(string searchTerm);
        Task<FamilyPrisonerLink> RequestLinkAsync(string userId, int prisonerId, string relationship);
        Task VerifyLinkAsync(int linkId, string adminId);
        Task RejectLinkAsync(int linkId, string adminId);
        Task<IEnumerable<FamilyPrisonerLink>> GetUserLinkedPrisonersAsync(string userId);
        Task<IEnumerable<FamilyPrisonerLink>> GetPendingLinksAsync();
        Task UpdatePrisonerStatusAsync(int prisonerId, string newStatus);
    }
}
