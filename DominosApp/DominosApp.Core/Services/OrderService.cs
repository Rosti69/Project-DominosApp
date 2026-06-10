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
            var pizzaIds = model.Items.Select(i => i.PizzaId).ToList();
            var pizzas = await _repository.AllAsNoTracking<Pizza>()
                .Where(p => pizzaIds.Contains(p.Id) && p.IsAvailable)
                .ToListAsync();

            if (pizzas.Count != pizzaIds.Count)
                throw new NotFoundException("One or more pizzas were not found or are unavailable.");

            var order = new Order
            {
                UserId = userId,
                OrderedAt = DateTime.UtcNow
            };

            foreach (var item in model.Items)
            {
                var pizza = pizzas.First(p => p.Id == item.PizzaId);
                order.OrderPizzas.Add(new OrderPizza
                {
                    PizzaId = item.PizzaId,
                    Quantity = item.Quantity
                });
                order.TotalPrice += pizza.Price * item.Quantity;
            }

            await _repository.AddAsync(order);
            await _repository.SaveChangesAsync();
            return order.Id;
        }

        public async Task<bool> DeleteAsync(string id)
        {
            var order = await _repository.All<Order>()
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
                throw new NotFoundException($"Order with id {id} was not found.");

            _repository.Delete(order);
            await _repository.SaveChangesAsync();
            return true;
        }
    }
}
