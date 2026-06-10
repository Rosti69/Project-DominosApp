using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DominosApp.Infrastructure.Data.Models
{
    public class Pizza
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [StringLength(50, MinimumLength = 2)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; } = 9.99m;

        [StringLength(250)]
        public string Description { get; set; } = string.Empty;

        [StringLength(250)]
        public string ImageUrl { get; set; } = string.Empty;

        public bool IsAvailable { get; set; } = true;

        public ICollection<OrderPizza> OrderPizzas { get; set; } = new List<OrderPizza>();
    }
}
