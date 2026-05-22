namespace HRService.Dto
{
    public class RegisterEmployeeTaskResponse
    {
        public int UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string TemporaryPassword { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}