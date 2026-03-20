using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PrisonerPortal.Data;
using PrisonerPortal.Models.Entities;
using PrisonerPortal.Models.ViewModels;
using PrisonerPortal.Services;
using System.Security.Claims;

namespace PrisonerPortal.Controllers
{
    [Authorize(Roles = "Family")]
    public class FamilyController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<FamilyController> _logger;
        private readonly IPrisonerService _prisonerService;
        private readonly IAppointmentService _appointmentService;
        private readonly ISupportRequestService _requestService;
        private readonly ApplicationDbContext _context;

        public FamilyController(
            UserManager<ApplicationUser> userManager,
            ILogger<FamilyController> logger,
            IPrisonerService prisonerService,
            IAppointmentService appointmentService,
            ISupportRequestService requestService,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _logger = logger;
            _prisonerService = prisonerService;
            _appointmentService = appointmentService;
            _requestService = requestService;
            _context = context;
        }

        public async Task<IActionResult> Dashboard()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return RedirectToAction("Login", "Account");

                var links = await _prisonerService.GetUserLinkedPrisonersAsync(user.Id);
                var appointments = await _appointmentService.GetUserAppointmentsAsync(user.Id);
                var requests = await _requestService.GetUserRequestsAsync(user.Id);

                ViewBag.LinkedCount = links?.Count() ?? 0;
                ViewBag.UpcomingVisits = appointments?.Count(a => a.Status == "Approved" && a.Slot != null && a.Slot.SlotDate >= DateTime.Today) ?? 0;
                ViewBag.OpenRequests = requests?.Count(r => r.Status == "Open" || r.Status == "InProgress") ?? 0;

                ViewBag.RecentAppointments = appointments?.OrderByDescending(a => a.BookedAt).Take(3).ToList() ?? new List<Appointment>();
                ViewBag.RecentRequests = requests?.OrderByDescending(r => r.CreatedAt).Take(3).ToList() ?? new List<SupportRequest>();
                ViewBag.FullName = user.FullName;

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading family dashboard");
                TempData["ErrorMessage"] = "Failed to load dashboard. Please try again.";
                return View();
            }
        }

        public async Task<IActionResult> SearchPrisoner(string q)
        {
            try
            {
                ViewBag.SearchTerm = q;
                var results = await _prisonerService.SearchPrisonersAsync(q);
                
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    var userLinks = await _context.FamilyPrisonerLinks
                        .Where(l => l.UserId == user.Id)
                        .ToListAsync();
                    ViewBag.UserLinks = userLinks ?? new List<FamilyPrisonerLink>();
                }

                return View(results ?? new List<Prisoner>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching prisoners with query: {Query}", q);
                TempData["ErrorMessage"] = "Search failed.";
                return View(new List<Prisoner>());
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestLink(LinkRequestViewModel model)
        {
            try
            {
                if (!ModelState.IsValid) return RedirectToAction(nameof(SearchPrisoner));

                var user = await _userManager.GetUserAsync(User);
                if (user == null) return Unauthorized();

                // Prevent duplicate requests
                var exists = await _context.FamilyPrisonerLinks
                    .AnyAsync(l => l.UserId == user.Id && l.PrisonerId == model.PrisonerId);

                if (exists)
                {
                    TempData["ErrorMessage"] = "A link request for this prisoner already exists.";
                    return RedirectToAction(nameof(SearchPrisoner));
                }

                await _prisonerService.RequestLinkAsync(user.Id, model.PrisonerId, model.Relationship);
                TempData["SuccessMessage"] = "Link request submitted! Awaiting admin verification.";
                
                return RedirectToAction(nameof(SearchPrisoner));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error requesting link");
                TempData["ErrorMessage"] = "Failed to submit link request.";
                return RedirectToAction(nameof(SearchPrisoner));
            }
        }

        public async Task<IActionResult> PrisonerDetail(int id)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return Unauthorized();

                var link = await _context.FamilyPrisonerLinks
                    .FirstOrDefaultAsync(l => l.UserId == user.Id && l.PrisonerId == id && l.IsVerified);

                if (link == null)
                {
                    TempData["ErrorMessage"] = "You do not have a verified link with this prisoner.";
                    return RedirectToAction(nameof(Dashboard));
                }

                var prisoner = await _prisonerService.GetByIdAsync(id);
                if (prisoner == null) return NotFound("Prisoner not found");

                var upcomingVisit = await _context.Appointments
                    .Include(a => a.Slot)
                    .Where(a => a.UserId == user.Id && 
                                a.PrisonerId == id && 
                                a.Status == "Approved" && 
                                a.Slot.SlotDate >= DateTime.Today)
                    .OrderBy(a => a.Slot.SlotDate)
                    .FirstOrDefaultAsync();

                ViewBag.UpcomingVisit = upcomingVisit;
                return View(prisoner);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading prisoner details for ID: {Id}", id);
                TempData["ErrorMessage"] = "Failed to load prisoner details.";
                return RedirectToAction(nameof(Dashboard));
            }
        }

        [HttpGet]
        public async Task<IActionResult> BookVisit(int? prisonerId)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return Unauthorized();

                var links = await _prisonerService.GetUserLinkedPrisonersAsync(user.Id);
                if (links == null || !links.Any())
                {
                    TempData["ErrorMessage"] = "You must have a verified link with a prisoner before booking.";
                    return RedirectToAction(nameof(SearchPrisoner));
                }

                var slots = await _context.VisitSlots
                    .Where(s => s.IsActive && s.SlotDate >= DateTime.Today && s.CurrentBookings < s.MaxCapacity)
                    .OrderBy(s => s.SlotDate).ThenBy(s => s.StartTime)
                    .ToListAsync();

                var model = new BookVisitViewModel
                {
                    PrisonerId = prisonerId ?? 0,
                    VisitorNames = user.FullName,
                    NumVisitors = 1,
                    LinkedPrisoners = links,
                    AvailableSlots = slots ?? new List<VisitSlot>(),
                    Purpose = ""
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading booking page");
                TempData["ErrorMessage"] = "Failed to load booking page.";
                return RedirectToAction(nameof(Dashboard));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BookVisit(BookVisitViewModel model)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return Unauthorized();

                if (ModelState.IsValid)
                {
                    // Check if prisoner is linked and verified
                    var isLinked = await _context.FamilyPrisonerLinks
                        .AnyAsync(l => l.UserId == user.Id && l.PrisonerId == model.PrisonerId && l.IsVerified);
                    
                    if (!isLinked)
                    {
                        ModelState.AddModelError("PrisonerId", "You are not linked/verified with this prisoner.");
                    }
                    else
                    {
                        var appointment = await _appointmentService.BookVisitAsync(
                            user.Id, 
                            model.PrisonerId, 
                            model.SlotId, 
                            model.Purpose, 
                            model.NumVisitors, 
                            model.VisitorNames, 
                            model.Notes);

                        TempData["SuccessMessage"] = $"Booking submitted! Code: {appointment.AppointmentCode}";
                        return RedirectToAction(nameof(MyAppointments));
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Booking failed");
                ModelState.AddModelError("", ex.Message);
            }

            var currUser = await _userManager.GetUserAsync(User);
            model.LinkedPrisoners = await _prisonerService.GetUserLinkedPrisonersAsync(currUser?.Id ?? "");
            model.AvailableSlots = await _context.VisitSlots
                .Where(s => s.IsActive && s.SlotDate >= DateTime.Today && s.CurrentBookings < s.MaxCapacity)
                .OrderBy(s => s.SlotDate).ThenBy(s => s.StartTime)
                .ToListAsync();

            return View(model);
        }

        public async Task<IActionResult> MyAppointments()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return Unauthorized();

                var appointments = await _appointmentService.GetUserAppointmentsAsync(user.Id);
                return View(appointments ?? new List<Appointment>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching appointments");
                return View(new List<Appointment>());
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelAppointment(int id)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return Unauthorized();

                await _appointmentService.CancelAppointmentAsync(id, user.Id);
                TempData["SuccessMessage"] = "Appointment cancelled.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cancellation error for appointment ID: {Id}", id);
                TempData["ErrorMessage"] = ex.Message;
            }
            return RedirectToAction(nameof(MyAppointments));
        }

        [HttpGet]
        public async Task<IActionResult> SubmitRequest(int? prisonerId)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return Unauthorized();

                var links = await _prisonerService.GetUserLinkedPrisonersAsync(user.Id);
                if (links == null || !links.Any())
                {
                    TempData["ErrorMessage"] = "You must have a verified link with a prisoner before submitting requests.";
                    return RedirectToAction(nameof(SearchPrisoner));
                }

                var model = new SubmitRequestViewModel
                {
                    PrisonerId = prisonerId ?? 0,
                    LinkedPrisoners = links,
                    Category = "",
                    Subject = "",
                    Description = "",
                    Priority = "Medium"
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading request form");
                return RedirectToAction(nameof(Dashboard));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitRequest(SubmitRequestViewModel model)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return Unauthorized();

                if (ModelState.IsValid)
                {
                    // Check if prisoner is linked and verified
                    var isLinked = await _context.FamilyPrisonerLinks
                        .AnyAsync(l => l.UserId == user.Id && l.PrisonerId == model.PrisonerId && l.IsVerified);
                    
                    if (!isLinked)
                    {
                        ModelState.AddModelError("PrisonerId", "You are not linked/verified with this prisoner.");
                    }
                    else
                    {
                        var request = await _requestService.SubmitRequestAsync(
                            user.Id, 
                            model.PrisonerId ?? 0, 
                            model.Category, 
                            model.Subject, 
                            model.Description, 
                            model.Priority);

                        TempData["SuccessMessage"] = $"Support request submitted! Code: {request.RequestCode}";
                        return RedirectToAction(nameof(MyRequests));
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Form submission failed");
                ModelState.AddModelError("", "Failed to submit request. Please try again.");
            }

            var currUser = await _userManager.GetUserAsync(User);
            var currLinks = await _prisonerService.GetUserLinkedPrisonersAsync(currUser?.Id ?? "");
            model.LinkedPrisoners = currLinks;
            return View(model);
        }

        public async Task<IActionResult> MyRequests()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return Unauthorized();

                var requests = await _requestService.GetUserRequestsAsync(user.Id);
                return View(requests ?? new List<SupportRequest>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching requests");
                return View(new List<SupportRequest>());
            }
        }

        public async Task<IActionResult> RequestDetail(int id)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return Unauthorized();

                var request = await _requestService.GetByIdAsync(id);
                if (request == null || request.UserId != user.Id) return NotFound("Request not found");

                return View(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error viewing request {Id}", id);
                return RedirectToAction(nameof(MyRequests));
            }
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResolveRequest(int id)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return Unauthorized();

                var request = await _requestService.GetByIdAsync(id);
                if (request != null && request.UserId == user.Id)
                {
                    await _requestService.UpdateStatusAsync(id, "Resolved", user.Id);
                    TempData["SuccessMessage"] = "Request marked as resolved.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resolving request {Id}", id);
                TempData["ErrorMessage"] = "Action failed.";
            }
            return RedirectToAction(nameof(RequestDetail), new { id });
        }
    }
}

