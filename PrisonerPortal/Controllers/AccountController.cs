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
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<AccountController> _logger;
        private readonly INotificationService _notificationService;
        private readonly IAuditService _auditService;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ILogger<AccountController> logger,
            INotificationService notificationService,
            IAuditService auditService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            _notificationService = notificationService;
            _auditService = auditService;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            try
            {
                if (User.Identity != null && User.Identity.IsAuthenticated)
                {
                    if (User.IsInRole("Admin"))
                        return RedirectToAction("Dashboard", "Admin");
                    return RedirectToAction("Dashboard", "Family");
                }

                ViewData["ReturnUrl"] = returnUrl;
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in {Action}", nameof(Login));
                return View();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            try
            {
                if (!ModelState.IsValid) return View(model);

                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null)
                {
                    ModelState.AddModelError("", "Invalid email or password.");
                    return View(model);
                }

                if (!user.IsActive)
                {
                    ModelState.AddModelError("", "Your account has been deactivated. Contact admin.");
                    return View(model);
                }

                var result = await _signInManager.PasswordSignInAsync(
                    model.Email, 
                    model.Password, 
                    model.RememberMe, 
                    lockoutOnFailure: true);

                if (result.Succeeded)
                {
                    await _auditService.LogAsync(user.Id, "Login", "ApplicationUser", user.Id);
                    
                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                        return Redirect(returnUrl);

                    var roles = await _userManager.GetRolesAsync(user);
                    if (roles.Contains("Admin"))
                        return RedirectToAction("Dashboard", "Admin");
                    
                    return RedirectToAction("Dashboard", "Family");
                }

                if (result.IsLockedOut)
                {
                    ModelState.AddModelError("", "Account locked for 15 minutes due to too many failed attempts.");
                    return View(model);
                }

                ModelState.AddModelError("", "Invalid email or password.");
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login error");
                ModelState.AddModelError("", "Login failed. Please try again.");
                return View(model);
            }
        }

        [HttpGet]
        public IActionResult Register()
        {
            try
            {
                if (User.Identity != null && User.Identity.IsAuthenticated)
                {
                    if (User.IsInRole("Admin"))
                        return RedirectToAction("Dashboard", "Admin");
                    return RedirectToAction("Dashboard", "Family");
                }

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in {Action}", nameof(Register));
                return RedirectToAction("Login");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            try
            {
                if (!ModelState.IsValid) return View(model);

                // Check duplicate email
                var exists = await _userManager.FindByEmailAsync(model.Email);
                if (exists != null)
                {
                    ModelState.AddModelError("Email", "This email is already registered. Please login.");
                    return View(model);
                }

                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FullName = model.FullName,
                    PhoneNumber = model.PhoneNumber,
                    Address = model.Address,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    Role = "Family"
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, "Family");
                    
                    await _notificationService.CreateNotificationAsync(
                        user.Id,
                        "Welcome to PFSP",
                        "Welcome! Your account was created successfully.",
                        "Welcome");

                    await _auditService.LogAsync(user.Id, "Register", "ApplicationUser", user.Id);
                    
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    
                    TempData["SuccessMessage"] = "Welcome! Your account was created successfully.";
                    return RedirectToAction("Dashboard", "Family");
                }

                // Show each identity error clearly
                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error.Description);

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Registration error");
                ModelState.AddModelError("", "Registration failed. Please try again.");
                return View(model);
            }
        }

        [HttpGet]
        public async Task<JsonResult> CheckEmail(string email)
        {
            try
            {
                if (string.IsNullOrEmpty(email)) 
                    return Json(new { exists = false });
                var user = await _userManager.FindByEmailAsync(email);
                return Json(new { exists = user != null });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Email check error");
                return Json(new { exists = false });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                if (userId != null)
                    await _auditService.LogAsync(userId, "Logout", "ApplicationUser", userId);

                await _signInManager.SignOutAsync();
                TempData["SuccessMessage"] = "You have been logged out safely.";
                return RedirectToAction("Login", "Account");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Logout error");
                TempData["ErrorMessage"] = "Logout failed.";
                return RedirectToAction("Dashboard", "Home");
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return NotFound("User not found");

                var model = new UserProfileViewModel
                {
                    UserId = user.Id,
                    FullName = user.FullName,
                    PhoneNumber = user.PhoneNumber ?? "",
                    Address = user.Address ?? "",
                    Email = user.Email,
                    Role = user.Role
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in {Action}", nameof(Profile));
                TempData["ErrorMessage"] = "Could not load profile.";
                return RedirectToDashboard();
            }
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(UserProfileViewModel model)
        {
            try
            {
                if (!ModelState.IsValid) return View(model);

                var user = await _userManager.GetUserAsync(User);
                if (user == null) return NotFound("User not found");

                user.FullName = model.FullName;
                user.PhoneNumber = model.PhoneNumber;
                user.Address = model.Address;

                await _userManager.UpdateAsync(user);

                TempData["SuccessMessage"] = "Profile updated successfully.";
                return RedirectToAction(nameof(Profile));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Profile update error");
                TempData["ErrorMessage"] = "Failed to update profile.";
                return View(model);
            }
        }

        [Authorize]
        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View(new ChangePasswordViewModel());
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            try
            {
                if (!ModelState.IsValid) return View(model);

                var user = await _userManager.GetUserAsync(User);
                if (user == null) return NotFound("User not found");

                var result = await _userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);

                if (result.Succeeded)
                {
                    // Re-sign in so the session stays valid after password change
                    await _signInManager.RefreshSignInAsync(user);
                    TempData["SuccessMessage"] = "Password changed successfully!";
                    return RedirectToAction(nameof(Profile));
                }

                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error.Description);

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Change password error");
                ModelState.AddModelError("", "Failed to change password. Please try again.");
                return View(model);
            }
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        private IActionResult RedirectToDashboard()
        {
            if (User.IsInRole("Admin"))
                return RedirectToAction("Dashboard", "Admin");
            return RedirectToAction("Dashboard", "Family");
        }
    }
}

