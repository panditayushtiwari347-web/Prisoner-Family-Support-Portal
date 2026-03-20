namespace PrisonerPortal.Services
{
    public interface ICodeGeneratorService
    {
        string GeneratePrisonerCode();
        string GenerateAppointmentCode();
        string GenerateRequestCode();
        Task<string> GenerateAppointmentCodeAsync();
        Task<string> GenerateRequestCodeAsync();
    }
}
