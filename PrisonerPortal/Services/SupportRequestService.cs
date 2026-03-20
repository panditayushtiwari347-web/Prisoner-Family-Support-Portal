using Microsoft.EntityFrameworkCore;
using PrisonerPortal.Data;
using PrisonerPortal.Helpers;
using PrisonerPortal.Models.Entities;

namespace PrisonerPortal.Services
{
    public class SupportRequestService : ISupportRequestService
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService;

        public SupportRequestService(ApplicationDbContext context, INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        public async Task<SupportRequest> SubmitRequestAsync(string userId, int prisonerId, string category, string subject, string description, string priority)
        {
            var request = new SupportRequest
            {
                RequestCode = CodeGenerator.GenerateRequestCode(),
                UserId = userId,
                PrisonerId = prisonerId > 0 ? prisonerId : null,
                Category = category,
                Subject = subject,
                Description = description,
                Priority = priority,
                Status = "Open",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _context.SupportRequests.AddAsync(request);
            await _context.SaveChangesAsync();

            return request;
        }

        public async Task UpdateStatusAsync(int requestId, string status, string adminId)
        {
            var request = await _context.SupportRequests.FindAsync(requestId);
            if (request == null) return;

            request.Status = status;
            request.UpdatedAt = DateTime.UtcNow;
            
            if (status == "Resolved" || status == "Closed")
                request.ResolvedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await _notificationService.CreateNotificationAsync(
                request.UserId, 
                "Support Request Update", 
                $"Your request (Code: {request.RequestCode}) status changed to {status}.", 
                "RequestUpdate", 
                request.RequestId, 
                "SupportRequest");
        }

        public async Task AddAdminResponseAsync(int requestId, string response, string adminId)
        {
            var request = await _context.SupportRequests.FindAsync(requestId);
            if (request == null) return;

            request.AdminResponse = response;
            request.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            
            await _notificationService.CreateNotificationAsync(
                request.UserId, 
                "New Response on Support Request", 
                $"An admin has replied to your request (Code: {request.RequestCode}).", 
                "RequestUpdate", 
                request.RequestId, 
                "SupportRequest");
        }

        public async Task AssignAdminAsync(int requestId, string adminId)
        {
            var request = await _context.SupportRequests.FindAsync(requestId);
            if (request == null) return;

            request.AssignedAdminId = adminId;
            request.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<SupportRequest>> GetUserRequestsAsync(string userId)
        {
            return await _context.SupportRequests
                .Include(r => r.Prisoner)
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<SupportRequest>> GetAllRequestsAsync()
        {
            return await _context.SupportRequests
                .Include(r => r.Prisoner)
                .Include(r => r.User)
                .Include(r => r.AssignedAdmin)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<SupportRequest?> GetByIdAsync(int id)
        {
            return await _context.SupportRequests
                .Include(r => r.Prisoner)
                .Include(r => r.User)
                .Include(r => r.AssignedAdmin)
                .FirstOrDefaultAsync(r => r.RequestId == id);
        }
    }
}
