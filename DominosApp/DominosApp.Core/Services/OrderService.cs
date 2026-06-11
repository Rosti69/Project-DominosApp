using DominosApp.Core.Contracts;
using DominosApp.Core.Exceptions;
using DominosApp.Core.Models.Order;
using DominosApp.Infrastructure.Common;
using DominosApp.Infrastructure.Data.Models;
using Microsoft.EntityFrameworkCore;


namespace DominosApp.Core.Services
{
    public class OrderService : IOrderService
    {
        private readonly IRepository _repository;

        public OrderService(IRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<OrderViewModel>> GetAllAsync()
        {
            return await _repository.AllAsNoTracking<Order>()
                .Include(o => o.User)
                .Include(o => o.OrderPizzas)
                    .ThenInclude(op => op.Pizza)
                .Select(o => new OrderViewModel
                {
                    Id = o.Id,
                    UserId = o.UserId,
                    UserName = o.User != null ? o.User.UserName! : "",
                    OrderedAt = o.OrderedAt,
                    TotalPrice = o.TotalPrice,
                    Status = o.Status.ToString(),
                    Pizzas = o.OrderPizzas.Select(op => new OrderPizzaViewModel
                    {
                        PizzaId = op.PizzaId,
                        PizzaName = op.Pizza != null ? op.Pizza.Name : "",
                        Price = op.Pizza != null ? op.Pizza.Price : 0,
                        Quantity = op.Quantity
                    }).ToList()
                })
                .ToListAsync();
        }

        public async Task<IEnumerable<OrderViewModel>> GetByUserIdAsync(string userId)
        {
            return await _repository.AllAsNoTracking<Order>()
                .Where(o => o.UserId == userId)
                .Include(o => o.User)
                .Include(o => o.OrderPizzas)
                    .ThenInclude(op => op.Pizza)
                .Select(o => new OrderViewModel
                {
                    Id = o.Id,
                    UserId = o.UserId,
                    UserName = o.User != null ? o.User.UserName! : "",
                    OrderedAt = o.OrderedAt,
                    TotalPrice = o.TotalPrice,
                    Status = o.Status.ToString(),
                    Pizzas = o.OrderPizzas.Select(op => new OrderPizzaViewModel
                    {
                        PizzaId = op.PizzaId,
                        PizzaName = op.Pizza != null ? op.Pizza.Name : "",
                        Price = op.Pizza != null ? op.Pizza.Price : 0,
                        Quantity = op.Quantity
                    }).ToList()
                })
                .ToListAsync();
        }

        public async Task<OrderViewModel?> GetByIdAsync(string id)
        {
            var order = await _repository.AllAsNoTracking<Order>()
                .Include(o => o.User)
                .Include(o => o.OrderPizzas)
                    .ThenInclude(op => op.Pizza)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
                throw new NotFoundException($"Order with id {id} was not found.");

            return new OrderViewModel
            {
                Id = order.Id,
                UserId = order.UserId,
                UserName = order.User?.UserName ?? "",
                OrderedAt = order.OrderedAt,
                TotalPrice = order.TotalPrice,
                Status = order.Status.ToString(),
                Pizzas = order.OrderPizzas.Select(op => new OrderPizzaViewModel
                {
                    PizzaId = op.PizzaId,
                    PizzaName = op.Pizza?.Name ?? "",
                    Price = op.Pizza?.Price ?? 0,
                    Quantity = op.Quantity
                }).ToList()
            };
        }

        public async Task<string> CreateAsync(string userId, OrderFormModel model)
        {
            ValidateOrderItems(model);

            var pizzaIds = model.Items.Select(i => i.PizzaId).ToList();
            var pizzas = await _repository.AllAsNoTracking<Pizza>()
                .Where(p => pizzaIds.Contains(p.Id) && p.IsAvailable)
                .ToListAsync();

            if (pizzas.Count != pizzaIds.Count)
                throw new NotFoundException("One or more pizzas were not found or are unavailable.");

            var order = new Order
            {
                UserId = userId,
                OrderedAt = DateTime.UtcNow,
                Status = OrderStatus.Pending,
                TotalPrice = CalculateTotalPrice(pizzas, model)
            };

            foreach (var item in model.Items)
            {
                order.OrderPizzas.Add(new OrderPizza
                {
                    PizzaId = item.PizzaId,
                    Quantity = item.Quantity
                });
            }

            await _repository.AddAsync(order);
            await _repository.SaveChangesAsync();
            return order.Id;
        }

        public async Task UpdateStatusAsync(string id, int status)
        {
            var order = await _repository.All<Order>()
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
                throw new NotFoundException($"Order with id {id} was not found.");

            ValidateStatusTransition(order.Status, (OrderStatus)status);

            order.Status = (OrderStatus)status;
            await _repository.SaveChangesAsync();
        }

        public async Task<bool> DeleteAsync(string id)
        {
            var order = await _repository.All<Order>()
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
                throw new NotFoundException($"Order with id {id} was not found.");

            ValidateOrderDeletion(order.Status);

            _repository.Delete(order);
            await _repository.SaveChangesAsync();
            return true;
        }

        private const int MaxPizzasPerOrder = 10;
        private const int MaxQuantityPerItem = 10;

        private static void ValidateOrderItems(OrderFormModel model)
        {
            if (model.Items == null || !model.Items.Any())
                throw new InvalidOrderException("Order must contain at least one pizza.");

            if (model.Items.Count > MaxPizzasPerOrder)
                throw new InvalidOrderException($"Cannot order more than {MaxPizzasPerOrder} different pizzas in one order.");

            foreach (var item in model.Items)
            {
                if (item.Quantity < 1 || item.Quantity > MaxQuantityPerItem)
                    throw new InvalidOrderException($"Quantity must be between 1 and {MaxQuantityPerItem}.");
            }

            var duplicates = model.Items.GroupBy(i => i.PizzaId).Any(g => g.Count() > 1);
            if (duplicates)
                throw new InvalidOrderException("Cannot add the same pizza twice. Increase the quantity instead.");
        }

        private static void ValidateStatusTransition(OrderStatus currentStatus, OrderStatus newStatus)
        {
            if (currentStatus == OrderStatus.Delivered)
                throw new InvalidOrderException("Cannot change status of a delivered order.");

            if (currentStatus == OrderStatus.Cancelled)
                throw new InvalidOrderException("Cannot change status of a cancelled order.");

            if (newStatus == OrderStatus.Pending && currentStatus != OrderStatus.Pending)
                throw new InvalidOrderException("Cannot revert order back to Pending.");
        }

        private static void ValidateOrderDeletion(OrderStatus status)
        {
            if (status == OrderStatus.Delivered)
                throw new InvalidOrderException("Cannot delete a delivered order.");
        }

        private static decimal CalculateTotalPrice(List<Pizza> pizzas, OrderFormModel model)
        {
            decimal total = 0;
            foreach (var item in model.Items)
            {
                var pizza = pizzas.First(p => p.Id == item.PizzaId);
                total += pizza.Price * item.Quantity;
            }
            return total;
        }
    }
}