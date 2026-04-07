using Microsoft.EntityFrameworkCore;

namespace landing_page_backend.Services
{
    public interface IGenericService<T> where T : AuditableEntity
    {
        Task<List<T>> GetAllAsync();
        Task<T?> GetByIdAsync(Guid id);
        Task<T> CreateAsync(T entity);
        Task<bool> UpdateAsync(T entity);
        Task<bool> DeleteAsync(Guid id);
    }

    public class GenericService<T> : IGenericService<T> where T : AuditableEntity
    {
        protected readonly MyDbContext _db;

        public GenericService(MyDbContext db)
        {
            _db = db;
        }

        public virtual async Task<List<T>> GetAllAsync()
        {
            return await _db.Set<T>().ToListAsync();
        }

        public virtual async Task<T?> GetByIdAsync(Guid id)
        {
            return await _db.Set<T>()
                .Where(e => e.Id == id)
                .FirstOrDefaultAsync();
        }

        public virtual async Task<T> CreateAsync(T entity)
        {
            _db.Set<T>().Add(entity);
            await _db.SaveChangesAsync();
            return entity;
        }

        public virtual async Task<bool> UpdateAsync(T entity)
        {
            var existing = await _db.Set<T>().FindAsync(entity.Id);
            if (existing == null)
                return false;

            _db.Entry(existing).CurrentValues.SetValues(entity);
            await _db.SaveChangesAsync();
            return true;
        }

        public virtual async Task<bool> DeleteAsync(Guid id)
        {
            var entity = await _db.Set<T>().FindAsync(id);
            if (entity == null)
                return false;

            entity.IsDeleted = true;
            entity.DeletedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return true;
        }
    }
}
