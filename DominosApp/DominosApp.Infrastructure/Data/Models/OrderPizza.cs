namespace DominosApp.Infrastructure.Data.Models
{
    public class OrderPizza
    {
        public string OrderId { get; set; } = string.Empty;
        public Order? Order { get; set; }

        public string PizzaId { get; set; } = string.Empty;
        public Pizza? Pizza { get; set; }

        public int Quantity { get; set; } = 1;
    }
}
