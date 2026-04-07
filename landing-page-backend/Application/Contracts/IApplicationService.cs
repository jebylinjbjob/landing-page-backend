namespace landing_page_backend.Application.Contracts
{
    public interface IApplicationService<T> where T : AuditableEntity
    {
        Task<List<T>> GetListAsync();
        Task<T?> GetAsync(Guid id);
        Task<T> CreateAsync(T entity);
        Task<T> UpdateAsync(Guid id, T entity);
        Task DeleteAsync(Guid id);
    }
}
