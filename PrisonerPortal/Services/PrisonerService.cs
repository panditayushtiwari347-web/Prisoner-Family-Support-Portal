using Microsoft.EntityFrameworkCore;
using PrisonerPortal.Data;
using PrisonerPortal.Models.Entities;

namespace PrisonerPortal.Services
{
    public class PrisonerService : IPrisonerService
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService;

        public PrisonerService(ApplicationDbContext context, INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        public async Task<IEnumerable<Prisoner>> GetAllPrisonersAsync()
        {
            return await _context.Prisoners.OrderBy(p => p.FullName).ToListAsync();
        }

        public async Task<Prisoner?> GetByIdAsync(int id)
        {
            return await _context.Prisoners.FindAsync(id);
        }

        public async Task<IEnumerable<Prisoner>> SearchPrisonersAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm)) return Enumerable.Empty<Prisoner>();
            
            searchTerm = searchTerm.ToLower();
            return await _context.Prisoners
                .Where(p => p.FullName.ToLower().Contains(searchTerm) || 
                            p.PrisonerCode.ToLower().Contains(searchTerm) || 
                            p.CaseNumber.ToLower().Contains(searchTerm))
                .Take(20)
                .ToListAsync();
        }

        public async Task<FamilyPrisonerLink> RequestLinkAsync(string userId, int prisonerId, string relationship)
        {
            var existing = await _context.FamilyPrisonerLinks
                .FirstOrDefaultAsync(l => l.UserId == userId && l.PrisonerId == prisonerId);
                
            if (existing != null)
                throw new InvalidOperationException("Link already exists or requested.");

            var link = new FamilyPrisonerLink
            {
                UserId = userId,
                PrisonerId = prisonerId,
                Relationship = relationship,
                IsVerified = false,
                LinkedAt = DateTime.UtcNow
            };

            await _context.FamilyPrisonerLinks.AddAsync(link);
            await _context.SaveChangesAsync();
            return link;
        }

        public async Task VerifyLinkAsync(int linkId, string adminId)
        {
            var link = await _context.FamilyPrisonerLinks
                .Include(l => l.Prisoner)
                .FirstOrDefaultAsync(l => l.LinkId == linkId);
                
            if (link != null)
            {
                link.IsVerified = true;
                link.VerifiedByAdminId = adminId;
                await _context.SaveChangesAsync();

                await _notificationService.CreateNotificationAsync(
                    link.UserId, 
                    "Link Request Approved", 
                    $"Your request to link with {link.Prisoner?.FullName} has been verified.", 
                    "SystemAlert");
            }
        }

        public async Task RejectLinkAsync(int linkId, string adminId)
        {
            var link = await _context.FamilyPrisonerLinks
                .Include(l => l.Prisoner)
                .FirstOrDefaultAsync(l => l.LinkId == linkId);

            if (link != null)
            {
                var userId = link.UserId;
                var prisonerName = link.Prisoner?.FullName;
                
                _context.FamilyPrisonerLinks.Remove(link);
                await _context.SaveChangesAsync();

                await _notificationService.CreateNotificationAsync(
                    userId, 
                    "Link Request Rejected", 
                    $"Your request to link with {prisonerName} cannot be verified.", 
                    "SystemAlert");
            }
        }

        public async Task<IEnumerable<FamilyPrisonerLink>> GetUserLinkedPrisonersAsync(string userId)
        {
            return await _context.FamilyPrisonerLinks
                .Include(l => l.Prisoner)
                .Where(l => l.UserId == userId && l.IsVerified)
                .ToListAsync();
        }

        public async Task<IEnumerable<FamilyPrisonerLink>> GetPendingLinksAsync()
        {
            return await _context.FamilyPrisonerLinks
                .Include(l => l.User)
                .Include(l => l.Prisoner)
                .Where(l => !l.IsVerified)
                .OrderBy(l => l.LinkedAt)
                .ToListAsync();
        }

        public async Task UpdatePrisonerStatusAsync(int prisonerId, string newStatus)
        {
            var prisoner = await _context.Prisoners.FindAsync(prisonerId);
            if (prisoner != null)
            {
                prisoner.Status = newStatus;
                prisoner.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }
    }
}
