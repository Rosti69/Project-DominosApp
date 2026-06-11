using DominosApp.Core.Contracts;
using DominosApp.Core.Exceptions;
using DominosApp.Core.Models.Pizza;
using DominosApp.Infrastructure.Common;
using DominosApp.Infrastructure.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace DominosApp.Core.Services
{
    public class PizzaService : IPizzaService
    {
        private readonly IRepository _repository;

        public PizzaService(IRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<PizzaViewModel>> GetAllAsync()
        {
            return await _repository.AllAsNoTracking<Pizza>()
                .Where(p => p.IsAvailable)
                .Select(p => new PizzaViewModel
                {
                    Id = p.Id,
                    Name = p.Name,
                    Price = p.Price,
                    Description = p.Description,
                    ImageUrl = p.ImageUrl,
                    IsAvailable = p.IsAvailable
                })
                .ToListAsync();
        }

        public async Task<PizzaViewModel?> GetByIdAsync(string id)
        {
            var pizza = await _repository.AllAsNoTracking<Pizza>()
                .FirstOrDefaultAsync(p => p.Id == id);

            ValidatePizzaExists(pizza, id);
            ValidatePizzaAvailability(pizza!);

            return new PizzaViewModel
            {
                Id = pizza!.Id,
                Name = pizza.Name,
                Price = pizza.Price,
                Description = pizza.Description,
                ImageUrl = pizza.ImageUrl,
                IsAvailable = pizza.IsAvailable
            };
        }

        public async Task<string> CreateAsync(PizzaFormModel model)
        {
            var pizza = new Pizza
            {
                Name = model.Name,
                Price = model.Price,
                Description = model.Description,
                ImageUrl = model.ImageUrl,
                IsAvailable = model.IsAvailable
            };

            await _repository.AddAsync(pizza);
            await _repository.SaveChangesAsync();
            return pizza.Id;
        }

        public async Task UpdateAsync(string id, PizzaFormModel model)
        {
            var pizza = await _repository.All<Pizza>()
                .FirstOrDefaultAsync(p => p.Id == id);

            ValidatePizzaExists(pizza, id);

            pizza!.Name = model.Name;
            pizza.Price = model.Price;
            pizza.Description = model.Description;
            pizza.ImageUrl = model.ImageUrl;
            pizza.IsAvailable = model.IsAvailable;

            await _repository.SaveChangesAsync();
        }

        public async Task<bool> DeleteAsync(string id)
        {
            var pizza = await _repository.All<Pizza>()
                .FirstOrDefaultAsync(p => p.Id == id);

            ValidatePizzaExists(pizza, id);

            // Soft delete - just mark as unavailable
            pizza!.IsAvailable = false;
            await _repository.SaveChangesAsync();
            return true;
        }

        private static void ValidatePizzaExists(Pizza? pizza, string id)
        {
            if (pizza == null)
                throw new NotFoundException($"Pizza with id {id} was not found.");
        }

        private static void ValidatePizzaAvailability(Pizza pizza)
        {
            if (!pizza.IsAvailable)
                throw new NotFoundException($"Pizza '{pizza.Name}' is no longer available.");
        }
    }
}