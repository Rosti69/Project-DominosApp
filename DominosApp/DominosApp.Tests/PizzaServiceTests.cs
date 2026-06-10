using DominosApp.Core.Exceptions;
using DominosApp.Core.Models.Pizza;
using DominosApp.Core.Services;
using DominosApp.Infrastructure.Common;
using DominosApp.Infrastructure.Data.Models;
using MockQueryable.Moq;
using Moq;
using NUnit.Framework;

namespace DominosApp.Tests
{
    [TestFixture]
    public class PizzaServiceTests
    {
        private Mock<IRepository> _repoMock = null!;
        private PizzaService _service = null!;

        [SetUp]
        public void SetUp()
        {
            _repoMock = new Mock<IRepository>();
            _service = new PizzaService(_repoMock.Object);
        }

        private void SetupPizzas(List<Pizza> pizzas)
        {
            var mock = pizzas.AsQueryable().BuildMock();
            _repoMock.Setup(r => r.AllAsNoTracking<Pizza>()).Returns(mock);
            var mockTracked = pizzas.AsQueryable().BuildMock();
            _repoMock.Setup(r => r.All<Pizza>()).Returns(mockTracked);
        }

        [Test]
        public async Task GetAllAsync_ReturnsOnlyAvailablePizzas()
        {
            SetupPizzas(new List<Pizza>
            {
                new Pizza { Id = "1", Name = "Margherita", Price = 8.99m, IsAvailable = true },
                new Pizza { Id = "2", Name = "Old", Price = 5m, IsAvailable = false }
            });

            var result = (await _service.GetAllAsync()).ToList();

            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].Name, Is.EqualTo("Margherita"));
        }

        [Test]
        public async Task GetByIdAsync_WithValidId_ReturnsPizza()
        {
            SetupPizzas(new List<Pizza>
            {
                new Pizza { Id = "1", Name = "Pepperoni", Price = 10.99m, IsAvailable = true }
            });

            var result = await _service.GetByIdAsync("1");

            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Name, Is.EqualTo("Pepperoni"));
            Assert.That(result.Price, Is.EqualTo(10.99m));
        }

        [Test]
        public void GetByIdAsync_WithInvalidId_ThrowsNotFoundException()
        {
            SetupPizzas(new List<Pizza>());

            Assert.ThrowsAsync<NotFoundException>(() => _service.GetByIdAsync("bad-id"));
        }

        [Test]
        public async Task CreateAsync_ValidModel_CallsAddAndSave()
        {
            var model = new PizzaFormModel
            {
                Name = "New Pizza",
                Price = 11.99m,
                Description = "Test desc",
                IsAvailable = true
            };
            _repoMock.Setup(r => r.AddAsync(It.IsAny<Pizza>())).Returns(Task.CompletedTask);
            _repoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            var id = await _service.CreateAsync(model);

            Assert.That(id, Is.Not.Null.And.Not.Empty);
            _repoMock.Verify(r => r.AddAsync(It.IsAny<Pizza>()), Times.Once);
            _repoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Test]
        public async Task UpdateAsync_ValidId_UpdatesPizzaFields()
        {
            var pizza = new Pizza { Id = "1", Name = "Old Name", Price = 5m, IsAvailable = true };
            SetupPizzas(new List<Pizza> { pizza });
            _repoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            var model = new PizzaFormModel { Name = "New Name", Price = 15m, IsAvailable = true };
            await _service.UpdateAsync("1", model);

            Assert.That(pizza.Name, Is.EqualTo("New Name"));
            Assert.That(pizza.Price, Is.EqualTo(15m));
            _repoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Test]
        public void UpdateAsync_WithInvalidId_ThrowsNotFoundException()
        {
            SetupPizzas(new List<Pizza>());

            Assert.ThrowsAsync<NotFoundException>(() =>
                _service.UpdateAsync("bad-id", new PizzaFormModel { Name = "X", Price = 1m }));
        }

        [Test]
        public async Task DeleteAsync_ValidId_SetsPizzaUnavailable()
        {
            var pizza = new Pizza { Id = "1", Name = "Pizza", Price = 9m, IsAvailable = true };
            SetupPizzas(new List<Pizza> { pizza });
            _repoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            var result = await _service.DeleteAsync("1");

            Assert.That(result, Is.True);
            Assert.That(pizza.IsAvailable, Is.False);
        }

        [Test]
        public void DeleteAsync_WithInvalidId_ThrowsNotFoundException()
        {
            SetupPizzas(new List<Pizza>());

            Assert.ThrowsAsync<NotFoundException>(() => _service.DeleteAsync("bad-id"));
        }
    }
}
