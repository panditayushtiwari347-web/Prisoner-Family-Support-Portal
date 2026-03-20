using PrisonerPortal.Helpers;

namespace PrisonerPortal.Services
{
    public class CodeGeneratorService : ICodeGeneratorService
    {
        public string GeneratePrisonerCode() => CodeGenerator.GeneratePrisonerCode();
        public string GenerateAppointmentCode() => CodeGenerator.GenerateAppointmentCode();
        public string GenerateRequestCode() => CodeGenerator.GenerateRequestCode();

        public Task<string> GenerateAppointmentCodeAsync() => Task.FromResult(GenerateAppointmentCode());
        public Task<string> GenerateRequestCodeAsync() => Task.FromResult(GenerateRequestCode());
    }
}
