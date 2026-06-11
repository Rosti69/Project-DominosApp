using DominosApp.Core.Exceptions;
using DominosApp.Core.Models.Order;
using DominosApp.Core.Services;
using DominosApp.Infrastructure.Common;
using DominosApp.Infrastructure.Data.Models;
using MockQueryable.Moq;
using Moq;
using NUnit.Framework;

namespace DominosApp.Tests
{
    [TestFixture]
    public class OrderServiceTests
    {
        private Mock<IRepository> _repoMock = null!;
        private OrderService _service = null!;

        [SetUp]
        public void SetUp()
        {
            _repoMock = new Mock<IRepository>();
            _service = new OrderService(_repoMock.Object);
        }

        private void SetupOrders(List<Order> orders)
        {
            var mock = orders.AsQueryable().BuildMock();
            _repoMock.Setup(r => r.AllAsNoTracking<Order>()).Returns(mock);
            var mockTracked = orders.AsQueryable().BuildMock();
            _repoMock.Setup(r => r.All<Order>()).Returns(mockTracked);
        }

        private void SetupPizzas(List<Pizza> pizzas)
        {
            var mock = pizzas.AsQueryable().BuildMock();
            _repoMock.Setup(r => r.AllAsNoTracking<Pizza>()).Returns(mock);
        }

        private static Pizza CreatePizza(string id, string name, decimal price, bool isAvailable = true)
            => new Pizza { Id = id, Name = name, Price = price, IsAvailable = isAvailable };

        private static Order CreateOrder(string id, string userId, OrderStatus status, decimal total = 10m)
            => new Order { Id = id, UserId = userId, Status = status, TotalPrice = total };

        [Test]
        public async Task GetAllAsync_ReturnsAllOrders()
        {
            SetupOrders(new List<Order>
            {
                CreateOrder("1", "user1", OrderStatus.Pending),
                CreateOrder("2", "user2", OrderStatus.Delivered)
            });

            var result = (await _service.GetAllAsync()).ToList();

            Assert.That(result.Count, Is.EqualTo(2));
        }

        [Test]
        public async Task GetByUserIdAsync_ReturnsOnlyOrdersForThatUser()
        {
            SetupOrders(new List<Order>
            {
                CreateOrder("1", "user1", OrderStatus.Pending),
                CreateOrder("2", "user2", OrderStatus.Pending),
                CreateOrder("3", "user1", OrderStatus.Delivered)
            });

            var result = (await _service.GetByUserIdAsync("user1")).ToList();

            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result.All(o => o.UserId == "user1"), Is.True);
        }

        [Test]
        public async Task GetByIdAsync_WithValidId_ReturnsOrder()
        {
            SetupOrders(new List<Order>
            {
                CreateOrder("1", "user1", OrderStatus.Pending, 19.98m)
            });

            var result = await _service.GetByIdAsync("1");

            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Id, Is.EqualTo("1"));
            Assert.That(result.TotalPrice, Is.EqualTo(19.98m));
            Assert.That(result.Status, Is.EqualTo(OrderStatus.Pending.ToString()));
        }

        [Test]
        public void GetByIdAsync_WithInvalidId_ThrowsNotFoundException()
        {
            SetupOrders(new List<Order>());

            Assert.ThrowsAsync<NotFoundException>(() => _service.GetByIdAsync("bad-id"));
        }

        [Test]
        public async Task CreateAsync_ValidModel_CalculatesTotalAndSaves()
        {
            SetupPizzas(new List<Pizza>
            {
                CreatePizza("p1", "Margherita", 10m),
                CreatePizza("p2", "Pepperoni", 12m)
            });
            _repoMock.Setup(r => r.AddAsync(It.IsAny<Order>())).Returns(Task.CompletedTask);
            _repoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            var model = new OrderFormModel
            {
                Items = new List<OrderPizzaItem>
                {
                    new OrderPizzaItem { PizzaId = "p1", Quantity = 2 },
                    new OrderPizzaItem { PizzaId = "p2", Quantity = 1 }
                }
            };

            var id = await _service.CreateAsync("user1", model);

            Assert.That(id, Is.Not.Null.And.Not.Empty);
            _repoMock.Verify(r => r.AddAsync(It.Is<Order>(o =>
                o.UserId == "user1" &&
                o.TotalPrice == 32m &&
                o.Status == OrderStatus.Pending)), Times.Once);
            _repoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Test]
        public void CreateAsync_WithEmptyItems_ThrowsInvalidOrderException()
        {
            var model = new OrderFormModel { Items = new List<OrderPizzaItem>() };

            Assert.ThrowsAsync<InvalidOrderException>(() => _service.CreateAsync("user1", model));
        }

        [Test]
        public void CreateAsync_WithDuplicatePizzas_ThrowsInvalidOrderException()
        {
            var model = new OrderFormModel
            {
                Items = new List<OrderPizzaItem>
                {
                    new OrderPizzaItem { PizzaId = "p1", Quantity = 1 },
                    new OrderPizzaItem { PizzaId = "p1", Quantity = 2 }
                }
            };

            Assert.ThrowsAsync<InvalidOrderException>(() => _service.CreateAsync("user1", model));
        }

        [Test]
        public void CreateAsync_WithUnavailablePizza_ThrowsNotFoundException()
        {
            SetupPizzas(new List<Pizza>
            {
                CreatePizza("p1", "Margherita", 10m, isAvailable: false)
            });

            var model = new OrderFormModel
            {
                Items = new List<OrderPizzaItem>
                {
                    new OrderPizzaItem { PizzaId = "p1", Quantity = 1 }
                }
            };

            Assert.ThrowsAsync<NotFoundException>(() => _service.CreateAsync("user1", model));
        }

        [Test]
        public async Task UpdateStatusAsync_ValidTransition_UpdatesStatus()
        {
            var order = CreateOrder("1", "user1", OrderStatus.Pending);
            SetupOrders(new List<Order> { order });
            _repoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            await _service.UpdateStatusAsync("1", (int)OrderStatus.Preparing);

            Assert.That(order.Status, Is.EqualTo(OrderStatus.Preparing));
            _repoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Test]
        public void UpdateStatusAsync_WithInvalidId_ThrowsNotFoundException()
        {
            SetupOrders(new List<Order>());

            Assert.ThrowsAsync<NotFoundException>(() => _service.UpdateStatusAsync("bad-id", (int)OrderStatus.Preparing));
        }

        [Test]
        public void UpdateStatusAsync_OnDeliveredOrder_ThrowsInvalidOrderException()
        {
            var order = CreateOrder("1", "user1", OrderStatus.Delivered);
            SetupOrders(new List<Order> { order });

            Assert.ThrowsAsync<InvalidOrderException>(() => _service.UpdateStatusAsync("1", (int)OrderStatus.Preparing));
        }

        [Test]
        public void UpdateStatusAsync_RevertingToPending_ThrowsInvalidOrderException()
        {
            var order = CreateOrder("1", "user1", OrderStatus.Preparing);
            SetupOrders(new List<Order> { order });

            Assert.ThrowsAsync<InvalidOrderException>(() => _service.UpdateStatusAsync("1", (int)OrderStatus.Pending));
        }

        [Test]
        public async Task DeleteAsync_ValidId_DeletesOrder()
        {
            var order = CreateOrder("1", "user1", OrderStatus.Pending);
            SetupOrders(new List<Order> { order });
            _repoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            var result = await _service.DeleteAsync("1");

            Assert.That(result, Is.True);
            _repoMock.Verify(r => r.Delete(order), Times.Once);
            _repoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Test]
        public void DeleteAsync_WithInvalidId_ThrowsNotFoundException()
        {
            SetupOrders(new List<Order>());

            Assert.ThrowsAsync<NotFoundException>(() => _service.DeleteAsync("bad-id"));
        }

        [Test]
        public void DeleteAsync_OnDeliveredOrder_ThrowsInvalidOrderException()
        {
            var order = CreateOrder("1", "user1", OrderStatus.Delivered);
            SetupOrders(new List<Order> { order });

            Assert.ThrowsAsync<InvalidOrderException>(() => _service.DeleteAsync("1"));
        }
    }
}
