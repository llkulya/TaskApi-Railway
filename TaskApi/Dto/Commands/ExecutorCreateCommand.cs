using System.ComponentModel.DataAnnotations;

namespace TaskApi.Dto.Commands
{
    public class ExecutorCreateCommand
    {
        [Required(ErrorMessage = "First name is required")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required")]
        public string LastName { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = string.Empty;

        public string Role { get; set; } = "Developer"; 
    }
}