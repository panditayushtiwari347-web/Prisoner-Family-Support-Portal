using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PrisonerPortal.Data;
using PrisonerPortal.Models.Entities;
using PrisonerPortal.Models.ViewModels;
using PrisonerPortal.Services;

namespace PrisonerPortal.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<AdminController> _logger;
        private readonly IPrisonerService _prisonerService;
        private readonly IAppointmentService _appointmentService;
        private readonly ISupportRequestService _requestService;
        private readonly INotificationService _notificationService;

        public AdminController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<AdminController> logger,
            IPrisonerService prisonerService,
            IAppointmentService appointmentService,
            ISupportRequestService requestService,
            INotificationService notificationService)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
            _prisonerService = prisonerService;
            _appointmentService = appointmentService;
            _requestService = requestService;
            _notificationService = notificationService;
        }

        public async Task<IActionResult> Dashboard()
        {
            try
            {
                var model = new AdminDashboardViewModel();

                // Safe counts - never crash if table empty
                model.TotalFamilies = await _context.Users
                    .Where(u => u.IsActive && u.Role == "Family")
                    .CountAsync();

                model.TotalPrisoners = await _context.Prisoners
                    .CountAsync(p => p.Status == "Active");

                model.TodaysVisits = await _context.Appointments
                    .Include(a => a.Slot)
                    .CountAsync(a => 
                        a.Slot != null && 
                        a.Slot.SlotDate.Date == DateTime.Today &&
                        (a.Status == "Approved" || a.Status == "Pending"));

                model.OpenRequests = await _context.SupportRequests
                    .CountAsync(s => s.Status == "Open" || s.Status == "InProgress");

                model.PendingAppointments = await _context.Appointments
                    .Include(a => a.User)
                    .Include(a => a.Prisoner)
                    .Include(a => a.Slot)
                    .Where(a => a.Status == "Pending")
                    .OrderByDescending(a => a.BookedAt)
                    .Take(10)
                    .ToListAsync() ?? new List<Appointment>();

                model.RecentRequests = await _context.SupportRequests
                    .Include(s => s.User)
                    .Include(s => s.Prisoner)
                    .Where(s => s.Status == "Open" || s.Status == "InProgress")
                    .OrderByDescending(s => s.CreatedAt)
                    .Take(10)
                    .ToListAsync() ?? new List<SupportRequest>();

                model.PendingLinks = await _context.FamilyPrisonerLinks
                    .Include(l => l.User)
                    .Include(l => l.Prisoner)
                    .Where(l => !l.IsVerified)
                    .OrderByDescending(l => l.LinkedAt)
                    .Take(10)
                    .ToListAsync() ?? new List<FamilyPrisonerLink>();

                model.RecentUsers = await _context.Users
                    .OrderByDescending(u => u.CreatedAt)
                    .Take(5)
                    .ToListAsync() ?? new List<ApplicationUser>();

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Admin dashboard error");
                TempData["ErrorMessage"] = "Dashboard failed to load. Please refresh.";
                return View(new AdminDashboardViewModel());
            }
        }

        // --- FAMILY LINKS ---
        public async Task<IActionResult> FamilyLinks(string filter = "")
        {
            try
            {
                var query = _context.FamilyPrisonerLinks
                    .Include(l => l.User)
                    .Include(l => l.Prisoner)
                    .AsQueryable();

                if (filter == "verified")
                    query = query.Where(l => l.IsVerified);
                else if (filter == "pending")
                    query = query.Where(l => !l.IsVerified);

                var links = await query
                    .OrderByDescending(l => l.LinkedAt)
                    .ToListAsync();

                ViewBag.Filter = filter;
                return View(links);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching family links");
                TempData["ErrorMessage"] = "Failed to load family links.";
                return RedirectToAction(nameof(Dashboard));
            }
        }

        // --- USERS ---
        public async Task<IActionResult> Users(string role = "", string status = "")

        {
            try
            {
                var query = _userManager.Users.AsQueryable();
                
                if (!string.IsNullOrEmpty(status))
                {
                    bool isActive = status == "Active";
                    query = query.Where(u => u.IsActive == isActive);
                }

                if (!string.IsNullOrEmpty(role))
                {
                    query = query.Where(u => u.Role == role);
                }

                var users = await query.OrderByDescending(u => u.CreatedAt).ToListAsync();
                return View(users ?? new List<ApplicationUser>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching users");
                TempData["ErrorMessage"] = "Failed to load users.";
                return RedirectToAction(nameof(Dashboard));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleUserStatus(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id)) return NotFound("User ID is required");

                var user = await _userManager.FindByIdAsync(id);
                if (user != null && user.Role != "Admin") 
                {
                    user.IsActive = !user.IsActive;
                    await _userManager.UpdateAsync(user);
                    TempData["SuccessMessage"] = $"User status changed to {(user.IsActive ? "Active" : "Inactive")}.";
                }
                return RedirectToAction(nameof(Users));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling user status");
                TempData["ErrorMessage"] = "Failed to update user status.";
                return RedirectToAction(nameof(Users));
            }
        }

        // --- PRISONERS ---
        public async Task<IActionResult> Prisoners()
        {
            try
            {
                var prisoners = await _prisonerService.GetAllPrisonersAsync();
                return View(prisoners ?? new List<Prisoner>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching prisoners");
                TempData["ErrorMessage"] = "Failed to load prisoners.";
                return RedirectToAction(nameof(Dashboard));
            }
        }

        public IActionResult AddPrisoner() => View(new PrisonerViewModel { FullName = "", Gender = "Male", CaseNumber = "", CrimeCategory = "", CellBlock = "", CellNumber = "", Status = "Active", SentenceStartDate = DateTime.Today, SentenceEndDate = DateTime.Today.AddYears(1) });

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddPrisoner(PrisonerViewModel model)
        {
            try
            {
                if (!ModelState.IsValid) return View(model);

                var prisoner = new Prisoner
                {
                    PrisonerCode = await _context.Prisoners.AnyAsync() ? $"PRS-{DateTime.Now.Year}-{new Random().Next(1000, 9999)}" : "PRS-2024-1001",
                    FullName = model.FullName,
                    DateOfBirth = model.DateOfBirth,
                    Gender = model.Gender,
                    CaseNumber = model.CaseNumber,
                    CrimeCategory = model.CrimeCategory,
                    SentenceStartDate = model.SentenceStartDate,
                    SentenceEndDate = model.SentenceEndDate,
                    CellBlock = model.CellBlock,
                    CellNumber = model.CellNumber,
                    Status = model.Status,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _context.Prisoners.AddAsync(prisoner);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Prisoner added successfully.";
                return RedirectToAction(nameof(Prisoners));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding prisoner");
                TempData["ErrorMessage"] = "Failed to add prisoner.";
                return View(model);
            }
        }

        public async Task<IActionResult> ViewPrisoner(int id)
        {
            try
            {
                var prisoner = await _context.Prisoners
                    .Include(p => p.FamilyLinks).ThenInclude(l => l.User)
                    .Include(p => p.Appointments).ThenInclude(a => a.User)
                    .Include(p => p.SupportRequests)
                    .FirstOrDefaultAsync(p => p.PrisonerId == id);

                if (prisoner == null) return NotFound("Prisoner not found");
                return View(prisoner);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error viewing prisoner");
                TempData["ErrorMessage"] = "Failed to load prisoner details.";
                return RedirectToAction(nameof(Prisoners));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePrisonerStatus(int id, string status)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(status))
                {
                    TempData["ErrorMessage"] = "Status cannot be empty.";
                    return RedirectToAction(nameof(ViewPrisoner), new { id });
                }

                await _prisonerService.UpdatePrisonerStatusAsync(id, status);
                TempData["SuccessMessage"] = $"Prisoner status updated to {status}.";
                return RedirectToAction(nameof(ViewPrisoner), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating prisoner status");
                TempData["ErrorMessage"] = "Failed to update status.";
                return RedirectToAction(nameof(ViewPrisoner), new { id });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyLink(int id)
        {
            try
            {
                var adminId = _userManager.GetUserId(User);
                if (adminId == null) return Unauthorized();

                await _prisonerService.VerifyLinkAsync(id, adminId);
                
                TempData["SuccessMessage"] = "Family link verified successfully.";
                return RedirectToAction(nameof(Dashboard));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Verify link error");
                TempData["ErrorMessage"] = "Verification failed. Try again.";
                return RedirectToAction(nameof(Dashboard));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectLink(int id)
        {
            try
            {
                var adminId = _userManager.GetUserId(User);
                if (adminId != null)
                    await _prisonerService.RejectLinkAsync(id, adminId);
                
                TempData["SuccessMessage"] = "Link request rejected.";
                return RedirectToAction(nameof(Dashboard));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Reject link error");
                TempData["ErrorMessage"] = "Action failed.";
                return RedirectToAction(nameof(Dashboard));
            }
        }

        // --- APPOINTMENTS ---
        public async Task<IActionResult> Appointments()
        {
            try
            {
                var apps = await _appointmentService.GetAllAppointmentsAsync();
                return View(apps ?? new List<Appointment>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching appointments");
                TempData["ErrorMessage"] = "Failed to load appointments.";
                return RedirectToAction(nameof(Dashboard));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveAppointment(int id)
        {
            try
            {
                var adminId = _userManager.GetUserId(User);
                if (adminId == null) return Unauthorized();

                await _appointmentService.ApproveAppointmentAsync(id, adminId);
                TempData["SuccessMessage"] = "Appointment approved successfully.";
                return RedirectToAction(nameof(Appointments));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Approve appointment error");
                TempData["ErrorMessage"] = ex.Message ?? "Failed to approve. Please try again.";
                return RedirectToAction(nameof(Appointments));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectAppointment(int id, string reason)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(reason))
                {
                    TempData["ErrorMessage"] = "Rejection reason is required.";
                    return RedirectToAction(nameof(Appointments));
                }

                var adminId = _userManager.GetUserId(User);
                if (adminId == null) return Unauthorized();

                await _appointmentService.RejectAppointmentAsync(id, adminId, reason);
                TempData["SuccessMessage"] = "Appointment rejected.";
                return RedirectToAction(nameof(Appointments));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Reject appointment error");
                TempData["ErrorMessage"] = "Failed to reject. Please try again.";
                return RedirectToAction(nameof(Appointments));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteAppointment(int id)
        {
            try
            {
                var adminId = _userManager.GetUserId(User);
                if (adminId != null)
                    await _appointmentService.CompleteAppointmentAsync(id, adminId);
                
                TempData["SuccessMessage"] = "Appointment marked as completed.";
                return RedirectToAction(nameof(Appointments));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing appointment");
                TempData["ErrorMessage"] = "Action failed.";
                return RedirectToAction(nameof(Appointments));
            }
        }

        // --- VISIT SLOTS ---
        public async Task<IActionResult> VisitSlots()
        {
            try
            {
                var slots = await _context.VisitSlots
                    .Include(s => s.CreatedByAdmin)
                    .Where(s => s.SlotDate >= DateTime.Today.AddDays(-7))
                    .OrderByDescending(s => s.SlotDate).ThenBy(s => s.StartTime)
                    .ToListAsync();
                return View(slots ?? new List<VisitSlot>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching slots");
                TempData["ErrorMessage"] = "Failed to load visit slots.";
                return RedirectToAction(nameof(Dashboard));
            }
        }

        [HttpGet]
        public IActionResult CreateSlot() => View(new VisitSlotViewModel 
        { 
            SlotDate = DateTime.Today.AddDays(1), 
            StartTime = new TimeSpan(10,0,0), 
            EndTime = new TimeSpan(11,0,0),
            MaxCapacity = 10
        });

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSlot(VisitSlotViewModel model)
        {
            try
            {
                if (!ModelState.IsValid) return View(model);

                var adminId = _userManager.GetUserId(User);
                var slot = new VisitSlot
                {
                    SlotDate = model.SlotDate,
                    StartTime = model.StartTime,
                    EndTime = model.EndTime,
                    MaxCapacity = model.MaxCapacity,
                    CreatedByAdminId = adminId!,
                    IsActive = true
                };
                await _context.VisitSlots.AddAsync(slot);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Slot created successfully.";
                return RedirectToAction(nameof(VisitSlots));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating slot");
                TempData["ErrorMessage"] = "Failed to create slot.";
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSlot(int id)
        {
            try
            {
                var slot = await _context.VisitSlots.FindAsync(id);
                if (slot == null) return NotFound("Slot not found");

                if (slot.CurrentBookings == 0)
                {
                    _context.VisitSlots.Remove(slot);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Slot deleted.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Cannot delete a slot with active bookings.";
                }
                return RedirectToAction(nameof(VisitSlots));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting slot");
                TempData["ErrorMessage"] = "Action failed.";
                return RedirectToAction(nameof(VisitSlots));
            }
        }

        // --- SUPPORT REQUESTS ---
        public async Task<IActionResult> Requests()
        {
            try
            {
                var requests = await _requestService.GetAllRequestsAsync();
                return View(requests ?? new List<SupportRequest>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching requests");
                TempData["ErrorMessage"] = "Failed to load requests.";
                return RedirectToAction(nameof(Dashboard));
            }
        }

        public async Task<IActionResult> RequestDetail(int id)
        {
            try
            {
                var request = await _requestService.GetByIdAsync(id);
                if (request == null) return NotFound("Request not found");
                return View(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching request detail");
                TempData["ErrorMessage"] = "Failed to load request details.";
                return RedirectToAction(nameof(Requests));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateRequestStatus(AdminResponseViewModel model)
        {
            try
            {
                var adminId = _userManager.GetUserId(User);
                if (adminId == null) return Unauthorized();
                
                if (!ModelState.IsValid) return RedirectToAction(nameof(RequestDetail), new { id = model.RequestId });

                await _requestService.UpdateStatusAsync(model.RequestId, model.Status, adminId);
                
                if (!string.IsNullOrWhiteSpace(model.Response))
                {
                    await _requestService.AddAdminResponseAsync(model.RequestId, model.Response, adminId);
                }

                if (string.IsNullOrEmpty(adminId)) return Unauthorized();

                await _requestService.AssignAdminAsync(model.RequestId, adminId);

                TempData["SuccessMessage"] = "Request updated successfully.";
                return RedirectToAction(nameof(RequestDetail), new { id = model.RequestId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating request");
                TempData["ErrorMessage"] = "Failed to update request.";
                return RedirectToAction(nameof(Requests));
            }
        }
    }
}

