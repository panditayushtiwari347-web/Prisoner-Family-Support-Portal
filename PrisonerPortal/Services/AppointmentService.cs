using Microsoft.EntityFrameworkCore;
using PrisonerPortal.Data;
using PrisonerPortal.Helpers;
using PrisonerPortal.Models.Entities;

namespace PrisonerPortal.Services
{
    public class AppointmentService : IAppointmentService
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly IEmailService _emailService;

        public AppointmentService(ApplicationDbContext context, INotificationService notificationService, IEmailService emailService)
        {
            _context = context;
            _notificationService = notificationService;
            _emailService = emailService;
        }

        public async Task<Appointment> BookVisitAsync(string userId, int prisonerId, int slotId, string purpose, int numVisitors, string visitorNames, string? notes)
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var hasExisting = await _context.Appointments
                        .AnyAsync(a => a.UserId == userId && a.SlotId == slotId && (a.Status == "Pending" || a.Status == "Approved"));
                    
                    if (hasExisting)
                        throw new InvalidOperationException("You already have a Pending or Approved appointment for this slot.");

                    var slot = await _context.VisitSlots.FindAsync(slotId);
                    if (slot == null || slot.CurrentBookings >= slot.MaxCapacity)
                        throw new InvalidOperationException("This slot is full or does not exist.");

                    var appointment = new Appointment
                    {
                        AppointmentCode = CodeGenerator.GenerateAppointmentCode(),
                        UserId = userId,
                        PrisonerId = prisonerId,
                        SlotId = slotId,
                        Purpose = purpose,
                        NumVisitors = numVisitors,
                        VisitorNames = visitorNames,
                        AdminNotes = notes,
                        Status = "Pending",
                        BookedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    await _context.Appointments.AddAsync(appointment);
                    await _context.SaveChangesAsync();
                    
                    await transaction.CommitAsync();
                    return appointment;
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });
        }

        public async Task ApproveAppointmentAsync(int appointmentId, string adminId)
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var appointment = await _context.Appointments
                        .Include(a => a.Slot)
                        .Include(a => a.User)
                        .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId);
                        
                    if (appointment == null || appointment.Status != "Pending")
                        throw new InvalidOperationException("Invalid appointment or status.");

                    if (appointment.Slot!.CurrentBookings >= appointment.Slot.MaxCapacity)
                        throw new InvalidOperationException("Slot is now full.");

                    appointment.Status = "Approved";
                    appointment.UpdatedAt = DateTime.UtcNow;
                    appointment.Slot.CurrentBookings++;

                    await _context.SaveChangesAsync();
                    
                    await _notificationService.CreateNotificationAsync(
                        appointment.UserId, 
                        "Appointment Approved", 
                        $"Your visit (Code: {appointment.AppointmentCode}) has been approved.", 
                        "BookingUpdate", 
                        appointment.AppointmentId, 
                        "Appointment");

                    await transaction.CommitAsync();
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });
        }

        public async Task RejectAppointmentAsync(int appointmentId, string adminId, string reason)
        {
            var appointment = await _context.Appointments.FindAsync(appointmentId);
            if (appointment == null || appointment.Status != "Pending") return;

            appointment.Status = "Rejected";
            appointment.RejectionReason = reason;
            appointment.UpdatedAt = DateTime.UtcNow;
            
            await _context.SaveChangesAsync();

            await _notificationService.CreateNotificationAsync(
                appointment.UserId, 
                "Appointment Rejected", 
                $"Your visit (Code: {appointment.AppointmentCode}) was rejected. Reason: {reason}", 
                "BookingUpdate", 
                appointment.AppointmentId, 
                "Appointment");
        }

        public async Task CancelAppointmentAsync(int appointmentId, string userId)
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var appointment = await _context.Appointments
                        .Include(a => a.Slot)
                        .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId && a.UserId == userId);
                        
                    if (appointment == null || (appointment.Status != "Pending" && appointment.Status != "Approved"))
                        throw new InvalidOperationException("Cannot cancel this appointment.");

                    if (appointment.Status == "Approved" && appointment.Slot != null)
                    {
                        appointment.Slot.CurrentBookings--;
                    }

                    appointment.Status = "Cancelled";
                    appointment.UpdatedAt = DateTime.UtcNow;

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });
        }

        public async Task CompleteAppointmentAsync(int appointmentId, string adminId)
        {
            var appointment = await _context.Appointments.FindAsync(appointmentId);
            if (appointment == null || appointment.Status != "Approved") return;

            appointment.Status = "Completed";
            appointment.UpdatedAt = DateTime.UtcNow;
            
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<Appointment>> GetUserAppointmentsAsync(string userId)
        {
            return await _context.Appointments
                .Include(a => a.Prisoner)
                .Include(a => a.Slot)
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.BookedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Appointment>> GetAllAppointmentsAsync()
        {
            return await _context.Appointments
                .Include(a => a.Prisoner)
                .Include(a => a.Slot)
                .Include(a => a.User)
                .OrderByDescending(a => a.BookedAt)
                .ToListAsync();
        }

        public async Task<Appointment?> GetByIdAsync(int id)
        {
            return await _context.Appointments
                .Include(a => a.Prisoner)
                .Include(a => a.Slot)
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.AppointmentId == id);
        }
    }
}
