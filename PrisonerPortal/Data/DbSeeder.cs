using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PrisonerPortal.Models.Entities;

namespace PrisonerPortal.Data
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            // Seed Roles
            string[] roles = { "Admin", "Family" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));
            }

            // Seed Admin User
            var adminEmail = "admin@prisonerportal.gov";
            if (await userManager.FindByEmailAsync(adminEmail) == null)
            {
                var admin = new ApplicationUser {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = "System Administrator",
                    EmailConfirmed = true,
                    IsActive = true,
                    Role = "Admin", // EXPLICITLY SET ROLE (defaults to Family)
                    CreatedAt = DateTime.UtcNow
                };
                var result = await userManager.CreateAsync(admin, "Admin@123!");
                if (result.Succeeded)
                    await userManager.AddToRoleAsync(admin, "Admin");
            }

            // Fix any existing Admin users whose Role property might have defaulted to "Family"
            var existingAdmins = await userManager.GetUsersInRoleAsync("Admin");
            foreach (var a in existingAdmins)
            {
                if (a.Role != "Admin")
                {
                    a.Role = "Admin";
                    await userManager.UpdateAsync(a);
                }
            }

            // Seed Demo Family User
            var familyEmail = "family@test.com";
            if (await userManager.FindByEmailAsync(familyEmail) == null)
            {
                var family = new ApplicationUser {
                    UserName = familyEmail,
                    Email = familyEmail,
                    FullName = "Demo Family User",
                    PhoneNumber = "9876543210",
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                var result = await userManager.CreateAsync(family, "Family@123!");
                if (result.Succeeded)
                    await userManager.AddToRoleAsync(family, "Family");
            }

            // Seed Sample Prisoners (only if none exist)
            if (!context.Prisoners.Any())
            {
                var prisoners = new List<Prisoner> {
                    new Prisoner {
                        PrisonerCode = "PRS-2024-0001",
                        FullName = "Ramesh Kumar",
                        DateOfBirth = new DateTime(1985, 4, 12),
                        Gender = "Male",
                        CaseNumber = "CASE-2024-001",
                        CrimeCategory = "Theft",
                        SentenceStartDate = new DateTime(2024, 1, 15),
                        SentenceEndDate = new DateTime(2027, 1, 15),
                        CellBlock = "Block-A",
                        CellNumber = "A-101",
                        Status = "Active",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    new Prisoner {
                        PrisonerCode = "PRS-2024-0002",
                        FullName = "Suresh Patel",
                        DateOfBirth = new DateTime(1978, 8, 22),
                        Gender = "Male",
                        CaseNumber = "CASE-2024-002",
                        CrimeCategory = "Fraud",
                        SentenceStartDate = new DateTime(2024, 3, 10),
                        SentenceEndDate = new DateTime(2028, 3, 10),
                        CellBlock = "Block-B",
                        CellNumber = "B-205",
                        Status = "Active",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    new Prisoner {
                        PrisonerCode = "PRS-2024-0003",
                        FullName = "Anjali Singh",
                        DateOfBirth = new DateTime(1992, 12, 5),
                        Gender = "Female",
                        CaseNumber = "CASE-2024-003",
                        CrimeCategory = "Drug Offense",
                        SentenceStartDate = new DateTime(2024, 6, 1),
                        SentenceEndDate = new DateTime(2026, 6, 1),
                        CellBlock = "Block-C",
                        CellNumber = "C-310",
                        Status = "Active",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    }
                };
                context.Prisoners.AddRange(prisoners);
            }

            // Seed Visit Slots (next 30 weekdays, two slots per day)
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            var adminId = adminUser?.Id ?? string.Empty;
            
            if (!context.VisitSlots.Any() && !string.IsNullOrEmpty(adminId))
            {
                var slots = new List<VisitSlot>();
                var date = DateTime.Today;
                int daysAdded = 0;
                while (daysAdded < 30)
                {
                    if (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday)
                    {
                        slots.Add(new VisitSlot {
                            SlotDate = date,
                            StartTime = new TimeSpan(10, 0, 0),
                            EndTime = new TimeSpan(11, 0, 0),
                            MaxCapacity = 10,
                            CurrentBookings = 0,
                            IsActive = true,
                            CreatedByAdminId = adminId,
                            CreatedAt = DateTime.UtcNow
                        });
                        slots.Add(new VisitSlot {
                            SlotDate = date,
                            StartTime = new TimeSpan(14, 0, 0),
                            EndTime = new TimeSpan(15, 0, 0),
                            MaxCapacity = 10,
                            CurrentBookings = 0,
                            IsActive = true,
                            CreatedByAdminId = adminId,
                            CreatedAt = DateTime.UtcNow
                        });
                        daysAdded++;
                    }
                    date = date.AddDays(1);
                }
                context.VisitSlots.AddRange(slots);
            }

            await context.SaveChangesAsync();
            // 5. Seed Family-Prisoner Link for Demo
            var familyUser = await userManager.FindByEmailAsync("family@test.com");
            var samplePrisoner = await context.Prisoners.FirstOrDefaultAsync();

            if (familyUser != null && samplePrisoner != null)
            {
                var existingLink = await context.FamilyPrisonerLinks
                    .AnyAsync(l => l.UserId == familyUser.Id && l.PrisonerId == samplePrisoner.PrisonerId);

                if (!existingLink)
                {
                    var link = new FamilyPrisonerLink
                    {
                        UserId = familyUser.Id,
                        PrisonerId = samplePrisoner.PrisonerId,
                        Relationship = "Spouse",
                        IsVerified = true,
                        LinkedAt = DateTime.UtcNow
                    };
                    context.FamilyPrisonerLinks.Add(link);
                    await context.SaveChangesAsync();
                }

                // Seed Sample Appointment for Dashboard
                var existingAppointment = await context.Appointments.AnyAsync(a => a.UserId == familyUser.Id);
                var slot = await context.VisitSlots.FirstOrDefaultAsync();

                if (!existingAppointment && slot != null)
                {
                    var appointment = new Appointment
                    {
                        AppointmentCode = "APT-DEMO-123",
                        UserId = familyUser.Id,
                        PrisonerId = samplePrisoner.PrisonerId,
                        SlotId = slot.SlotId,
                        Purpose = "Demo Visit",
                        NumVisitors = 1,
                        VisitorNames = familyUser.FullName ?? "Demo User",
                        Status = "Approved",
                        BookedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    context.Appointments.Add(appointment);
                    await context.SaveChangesAsync();
                }
            }
        }
    }
}

