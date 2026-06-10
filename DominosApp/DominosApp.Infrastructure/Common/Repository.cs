using Microsoft.EntityFrameworkCore;
using DominosApp.Infrastructure.Data;

namespace DominosApp.Infrastructure.Common
{
    public class Repository : IRepository
    {
        private readonly DominosAppDbContext _context;

        public Repository(DominosAppDbContext context)
        {
            _context = context;
        }

        public IQueryable<T> All<T>() where T : class
            => _context.Set<T>();

        public IQueryable<T> AllAsNoTracking<T>() where T : class
            => _context.Set<T>().AsNoTracking();

        public async Task AddAsync<T>(T entity) where T : class
            => await _context.Set<T>().AddAsync(entity);

        public void Delete<T>(T entity) where T : class
            => _context.Set<T>().Remove(entity);

        public async Task SaveChangesAsync()
            => await _context.SaveChangesAsync();
    }
}
