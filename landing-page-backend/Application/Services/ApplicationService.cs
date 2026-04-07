using landing_page_backend.Application.Contracts;
using landing_page_backend.Repositories;

namespace landing_page_backend.Application.Services
{
    public class ApplicationService<T> : IApplicationService<T> where T : AuditableEntity
    {
        protected readonly IUnitOfWork _unitOfWork;
        protected readonly IRepository<T> _repository;

        public ApplicationService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
            _repository = unitOfWork.Repository<T>();
        }

        public virtual async Task<List<T>> GetListAsync()
        {
            return await _repository.GetAllAsync();
        }

        public virtual async Task<T?> GetAsync(Guid id)
        {
            return await _repository.GetByIdAsync(id);
        }

        public virtual async Task<T> CreateAsync(T entity)
        {
            await _repository.InsertAsync(entity);
            await _unitOfWork.SaveChangesAsync();
            return entity;
        }

        public virtual async Task<T> UpdateAsync(Guid id, T entity)
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null)
                throw new KeyNotFoundException($"Entity with id {id} not found");

            entity.Id = id;
            await _repository.UpdateAsync(entity);
            await _unitOfWork.SaveChangesAsync();
            return entity;
        }

        public virtual async Task DeleteAsync(Guid id)
        {
            await _repository.DeleteAsync(id);
            await _unitOfWork.SaveChangesAsync();
        }
    }
}
