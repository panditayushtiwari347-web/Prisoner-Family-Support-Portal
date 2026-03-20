using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PrisonerPortal.Models.Entities;

namespace PrisonerPortal.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<Prisoner> Prisoners { get; set; }
        public DbSet<FamilyPrisonerLink> FamilyPrisonerLinks { get; set; }
        public DbSet<VisitSlot> VisitSlots { get; set; }
        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<SupportRequest> SupportRequests { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // ApplicationUser
            builder.Entity<ApplicationUser>(e => {
                e.Property(u => u.FullName).IsRequired().HasMaxLength(150);
                e.Property(u => u.Address).HasMaxLength(500);
                e.Property(u => u.IsActive).HasDefaultValue(true);
            });

            // Prisoner
            builder.Entity<Prisoner>(e => {
                e.HasKey(p => p.PrisonerId);
                e.HasIndex(p => p.PrisonerCode).IsUnique();
                e.HasIndex(p => p.CaseNumber).IsUnique();
                e.Property(p => p.FullName).IsRequired().HasMaxLength(150);
                e.Property(p => p.PrisonerCode).IsRequired().HasMaxLength(20);
                e.Property(p => p.Status).HasDefaultValue("Active");
            });

            // FamilyPrisonerLink
            builder.Entity<FamilyPrisonerLink>(e => {
                e.HasKey(f => f.LinkId);
                e.HasIndex(f => new { f.UserId, f.PrisonerId }).IsUnique();
                e.HasOne(f => f.User)
                    .WithMany(u => u.FamilyLinks)
                    .HasForeignKey(f => f.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
                e.HasOne(f => f.Prisoner)
                    .WithMany(p => p.FamilyLinks)
                    .HasForeignKey(f => f.PrisonerId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // VisitSlot
            builder.Entity<VisitSlot>(e => {
                e.HasKey(v => v.SlotId);
                e.Property(v => v.MaxCapacity).HasDefaultValue(10);
                e.Property(v => v.CurrentBookings).HasDefaultValue(0);
                e.Property(v => v.IsActive).HasDefaultValue(true);
            });

            // Appointment
            builder.Entity<Appointment>(e => {
                e.HasKey(a => a.AppointmentId);
                e.HasIndex(a => a.AppointmentCode).IsUnique();
                e.HasOne(a => a.User)
                    .WithMany(u => u.Appointments)
                    .HasForeignKey(a => a.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
                e.HasOne(a => a.Prisoner)
                    .WithMany(p => p.Appointments)
                    .HasForeignKey(a => a.PrisonerId)
                    .OnDelete(DeleteBehavior.Restrict);
                e.HasOne(a => a.Slot)
                    .WithMany(s => s.Appointments)
                    .HasForeignKey(a => a.SlotId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // SupportRequest
            builder.Entity<SupportRequest>(e => {
                e.HasKey(s => s.RequestId);
                e.HasIndex(s => s.RequestCode).IsUnique();
                e.HasOne(s => s.User)
                    .WithMany(u => u.SupportRequests)
                    .HasForeignKey(s => s.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
                e.HasOne(s => s.Prisoner)
                    .WithMany(p => p.SupportRequests)
                    .HasForeignKey(s => s.PrisonerId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Notification
            builder.Entity<Notification>(e => {
                e.HasKey(n => n.NotificationId);
                e.HasOne(n => n.User)
                    .WithMany(u => u.Notifications)
                    .HasForeignKey(n => n.UserId)
                    .OnDelete(DeleteBehavior.Restrict); // Changed from Cascade to prevent multiple paths
            });

            // AuditLog
            builder.Entity<AuditLog>(e => {
                e.HasKey(a => a.LogId);
            });
        }
    }
}
