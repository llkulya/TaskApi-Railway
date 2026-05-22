using System.ComponentModel.DataAnnotations;

namespace TaskApi.Dto.Commands
{
    public class RegisterEmployeeRequest
    {
        [Required]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Position { get; set; } = string.Empty;

        [Required]
        public string Department { get; set; } = string.Empty;
    }
}