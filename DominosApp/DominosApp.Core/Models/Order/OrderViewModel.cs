namespace DominosApp.Core.Models.Order
{
    public class OrderViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public DateTime OrderedAt { get; set; }
        public decimal TotalPrice { get; set; }
        public List<OrderPizzaViewModel> Pizzas { get; set; } = new();
    }

    public class OrderPizzaViewModel
    {
        public string PizzaId { get; set; } = string.Empty;
        public string PizzaName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Quantity { get; set; }
    }
}
