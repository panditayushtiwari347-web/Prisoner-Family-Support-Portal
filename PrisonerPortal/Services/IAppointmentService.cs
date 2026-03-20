using PrisonerPortal.Models.Entities;

namespace PrisonerPortal.Services
{
    public interface IAppointmentService
    {
        Task<Appointment> BookVisitAsync(string userId, int prisonerId, int slotId, string purpose, int numVisitors, string visitorNames, string? notes);
        Task ApproveAppointmentAsync(int appointmentId, string adminId);
        Task RejectAppointmentAsync(int appointmentId, string adminId, string reason);
        Task CancelAppointmentAsync(int appointmentId, string userId);
        Task CompleteAppointmentAsync(int appointmentId, string adminId);
        Task<IEnumerable<Appointment>> GetUserAppointmentsAsync(string userId);
        Task<IEnumerable<Appointment>> GetAllAppointmentsAsync();
        Task<Appointment?> GetByIdAsync(int id);
    }
}
