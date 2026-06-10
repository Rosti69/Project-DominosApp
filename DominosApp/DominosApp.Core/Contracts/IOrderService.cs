using DominosApp.Core.Models.Order;

namespace DominosApp.Core.Contracts
{
    public interface IOrderService
    {
        Task<IEnumerable<OrderViewModel>> GetAllAsync();
        Task<IEnumerable<OrderViewModel>> GetByUserIdAsync(string userId);
        Task<OrderViewModel?> GetByIdAsync(string id);
        Task<string> CreateAsync(string userId, OrderFormModel model);
        Task<bool> DeleteAsync(string id);
    }
}
