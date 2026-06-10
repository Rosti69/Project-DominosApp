namespace DominosApp.Infrastructure.Common
{
    public interface IRepository
    {
        IQueryable<T> All<T>() where T : class;
        IQueryable<T> AllAsNoTracking<T>() where T : class;
        Task AddAsync<T>(T entity) where T : class;
        void Delete<T>(T entity) where T : class;
        Task SaveChangesAsync();
    }
}
