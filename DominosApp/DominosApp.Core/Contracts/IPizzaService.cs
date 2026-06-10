using DominosApp.Core.Models.Pizza;

namespace DominosApp.Core.Contracts
{
    public interface IPizzaService
    {
        Task<IEnumerable<PizzaViewModel>> GetAllAsync();
        Task<PizzaViewModel?> GetByIdAsync(string id);
        Task<string> CreateAsync(PizzaFormModel model);
        Task UpdateAsync(string id, PizzaFormModel model);
        Task<bool> DeleteAsync(string id);
    }
}
