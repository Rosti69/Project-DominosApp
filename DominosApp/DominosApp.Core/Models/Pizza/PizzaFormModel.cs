using System.ComponentModel.DataAnnotations;

namespace DominosApp.Core.Models.Pizza
{
    public class PizzaFormModel
    {
        [Required(ErrorMessage = "Name is required.")]
        [StringLength(50, MinimumLength = 2)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Range(0.01, 999.99, ErrorMessage = "Price must be between 0.01 and 999.99")]
        public decimal Price { get; set; }

        [StringLength(250)]
        public string Description { get; set; } = string.Empty;

        [StringLength(250)]
        public string ImageUrl { get; set; } = string.Empty;

        public bool IsAvailable { get; set; } = true;
    }
}
