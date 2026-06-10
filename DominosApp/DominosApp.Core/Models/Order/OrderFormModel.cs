using System.ComponentModel.DataAnnotations;

namespace DominosApp.Core.Models.Order
{
    public class OrderFormModel
    {
        [Required]
        [MinLength(1, ErrorMessage = "At least one pizza is required.")]
        public List<OrderPizzaItem> Items { get; set; } = new();
    }

    public class OrderPizzaItem
    {
        [Required]
        public string PizzaId { get; set; } = string.Empty;

        [Range(1, 10)]
        public int Quantity { get; set; } = 1;
    }
}
