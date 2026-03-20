using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PrisonerPortal.Models.Entities;
using PrisonerPortal.Services;

namespace PrisonerPortal.Controllers
{
    [Authorize]
    public class NotificationsController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly INotificationService _notificationService;
        private readonly PrisonerPortal.Data.ApplicationDbContext _context;

        public NotificationsController(
            UserManager<ApplicationUser> userManager,
            INotificationService notificationService,
            PrisonerPortal.Data.ApplicationDbContext context)
        {
            _userManager = userManager;
            _notificationService = notificationService;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            var notifications = _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .ToList();

            return View(notifications);
        }

        [HttpPost]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            await _notificationService.MarkAsReadAsync(id);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = _userManager.GetUserId(User);
            if (userId != null)
                await _notificationService.MarkAllAsReadAsync(userId);
            return RedirectToAction(nameof(Index));
        }
        
        [HttpGet]
        public IActionResult GetUnreadCount()
        {
            var userId = _userManager.GetUserId(User);
            var count = _context.Notifications.Count(n => n.UserId == userId && !n.IsRead);
            return Json(new { count = count });
        }
    }
}
