using System.ComponentModel.DataAnnotations;

namespace DominosApp.Core.Models.Auth
{
    public class UpdateUserModel
    {
        [Required]
        [StringLength(100, MinimumLength = 2)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [StringLength(250)]
        public string Address { get; set; } = string.Empty;

        [Phone]
        public string PhoneNumber { get; set; } = string.Empty;
    }
}
