namespace TaskApi.Dto.Responses
{
    public class ExecutorDto
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public DateTime HireDate { get; set; }
        public List<string> AssignedTaskTitles { get; set; } = new();
    }
}