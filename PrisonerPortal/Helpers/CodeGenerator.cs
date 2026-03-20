namespace PrisonerPortal.Helpers
{
    public static class CodeGenerator
    {
        public static string GeneratePrisonerCode()
        {
            return $"PRS-{DateTime.Now.Year}-{new Random().Next(1000, 9999)}";
        }

        public static string GenerateAppointmentCode()
        {
            return $"APT-{DateTime.Now:yyyyMMdd}-{new Random().Next(1000, 9999)}";
        }

        public static string GenerateRequestCode()
        {
            return $"REQ-{DateTime.Now:yyyyMMdd}-{new Random().Next(1000, 9999)}";
        }
    }
}
