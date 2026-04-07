using landing_page_backend.DTOs;
using landing_page_backend.Repositories;

using Microsoft.EntityFrameworkCore;

namespace landing_page_backend.Services
{
    public interface ISitePageService
    {
        Task<List<SitePageResponseDto>> GetAllAsync(SitePageQueryDto query);
        Task<SitePageResponseDto?> GetByIdAsync(Guid id);
        Task<SitePageResponseDto> CreateAsync(CreateSitePageRequestDto request);
        Task<bool> UpdateAsync(Guid id, UpdateSitePageRequestDto request);
        Task<bool> DeleteAsync(Guid id);
        Task<SitePageResponseDto?> GetCurrentPublishedAsync();
        Task<bool> PublishAsync(Guid id, bool isPublished);
    }

    public class SitePageService : ISitePageService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IRepository<SitePage> _repository;

        public SitePageService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
            _repository = unitOfWork.Repository<SitePage>();
        }

        public async Task<List<SitePageResponseDto>> GetAllAsync(SitePageQueryDto query)
        {
            var dbQuery = _repository.GetQueryable();

            if (!string.IsNullOrWhiteSpace(query.Keyword))
            {
                var keyword = query.Keyword.Trim();
                dbQuery = dbQuery.Where(p => p.ContentJson.Contains(keyword));
            }

            var limit = query.Limit.GetValueOrDefault(100);
            limit = Math.Clamp(limit, 1, 500);

            return await dbQuery
                .OrderBy(p => p.Id)
                .Take(limit)
                .Select(p => new SitePageResponseDto
                {
                    Id = p.Id,
                    ContentJson = p.ContentJson,
                    Name = p.Name,
                    IsPublished = p.IsPublished
                })
                .ToListAsync();
        }

        public async Task<SitePageResponseDto?> GetByIdAsync(Guid id)
        {
            var page = await _repository.GetByIdAsync(id);
            if (page == null) return null;

            return new SitePageResponseDto
            {
                Id = page.Id,
                ContentJson = page.ContentJson,
                Name = page.Name,
                IsPublished = page.IsPublished
            };
        }

        public async Task<SitePageResponseDto> CreateAsync(CreateSitePageRequestDto request)
        {
            var page = new SitePage
            {
                ContentJson = request.ContentJson,
                Name = request.Name
            };

            await _repository.InsertAsync(page);
            await _unitOfWork.SaveChangesAsync();

            return new SitePageResponseDto
            {
                Id = page.Id,
                ContentJson = page.ContentJson,
                Name = page.Name,
                IsPublished = page.IsPublished
            };
        }

        public async Task<bool> UpdateAsync(Guid id, UpdateSitePageRequestDto request)
        {
            var page = await _repository.GetByIdAsync(id);
            if (page == null) return false;

            page.ContentJson = request.ContentJson;
            page.Name = request.Name;
            await _repository.UpdateAsync(page);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var page = await _repository.GetByIdAsync(id);
            if (page == null) return false;

            await _repository.DeleteAsync(id);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<SitePageResponseDto?> GetCurrentPublishedAsync()
        {
            var page = await _repository.GetQueryable()
                .Where(p => p.IsPublished)
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefaultAsync();
            if (page == null) return null;

            return new SitePageResponseDto
            {
                Id = page.Id,
                ContentJson = page.ContentJson,
                Name = page.Name,
                IsPublished = page.IsPublished
            };
        }

        public async Task<bool> PublishAsync(Guid id, bool isPublished)
        {
            var page = await _repository.GetByIdAsync(id);
            if (page == null) return false;

            if (isPublished)
            {
                await _repository.GetQueryable()
                    .Where(p => p.IsPublished && p.Id != id)
                    .ExecuteUpdateAsync(s => s.SetProperty(p => p.IsPublished, false));
            }

            page.IsPublished = isPublished;
            await _repository.UpdateAsync(page);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }
    }
}
