using System.ComponentModel.DataAnnotations;

namespace DominosApp.Core.Models.Order
{
    public class UpdateOrderStatusModel
    {
        [Required(ErrorMessage = "Status is required.")]
        [Range(0, 3, ErrorMessage = "Status must be between 0 (Pending) and 3 (Cancelled).")]
        public int Status { get; set; }
    }
}
