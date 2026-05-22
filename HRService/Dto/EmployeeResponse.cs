namespace HRService.Dto
{
    public class EmployeeResponse
    {
        public int EmployeeId { get; set; }
        public string Email { get; set; } = string.Empty;

        public bool TaskAccountCreated { get; set; }
        public int? TaskUserId { get; set; }

        public string Message { get; set; } = string.Empty;
    }
}