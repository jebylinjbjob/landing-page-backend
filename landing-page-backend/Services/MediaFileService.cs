using System.Security.Cryptography;

using landing_page_backend.DTOs;
using landing_page_backend.Repositories;

using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.EntityFrameworkCore;

namespace landing_page_backend.Services
{
    public interface IMediaFileService
    {
        Task<List<MediaFileResponseDto>> GetAllAsync(MediaFileQueryDto query);
        Task<MediaFileResponseDto?> GetByIdAsync(Guid id);
        Task<MediaFileUploadResultDto> UploadAsync(HttpRequest request, IFormFile file, string? description);
        Task<bool> UpdateDescriptionAsync(Guid id, UpdateMediaFileDescriptionRequestDto request);
        Task<bool> DeleteAsync(Guid id);
    }

    public class MediaFileService : IMediaFileService
    {
        private static readonly HashSet<string> AllowedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg",
            "image/png"
        };

        private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg",
            ".jpeg",
            ".png"
        };

        private readonly IUnitOfWork _unitOfWork;
        private readonly IRepository<MediaFile> _repository;
        private readonly IConfiguration _config;
        private readonly IWebHostEnvironment _env;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public MediaFileService(
            IUnitOfWork unitOfWork,
            IConfiguration config,
            IWebHostEnvironment env,
            IHttpContextAccessor httpContextAccessor)
        {
            _unitOfWork = unitOfWork;
            _repository = unitOfWork.Repository<MediaFile>();
            _config = config;
            _env = env;
            _httpContextAccessor = httpContextAccessor;
        }

        private static string ToPublicUrl(HttpRequest? request, string storagePath)
        {
            if (string.IsNullOrWhiteSpace(storagePath))
                return string.Empty;

            var path = "/" + storagePath.TrimStart('/').Replace('\\', '/');

            if (request == null)
                return path;

            return UriHelper.BuildAbsolute(request.Scheme, request.Host, request.PathBase, path);
        }

        public async Task<List<MediaFileResponseDto>> GetAllAsync(MediaFileQueryDto query)
        {
            var dbQuery = _repository.GetQueryable();

            if (!string.IsNullOrWhiteSpace(query.Keyword))
            {
                var keyword = query.Keyword.Trim();
                dbQuery = dbQuery.Where(m =>
                    (m.Description != null && m.Description.Contains(keyword)) ||
                    m.StoragePath.Contains(keyword));
            }

            if (!string.IsNullOrWhiteSpace(query.Hash))
            {
                var hash = query.Hash.Trim().ToLower();
                dbQuery = dbQuery.Where(m => m.Hash.Contains(hash));
            }

            var limit = query.Limit.GetValueOrDefault(100);
            limit = Math.Clamp(limit, 1, 500);

            var rows = await dbQuery
                .OrderByDescending(m => m.Id)
                .Take(limit)
                .Select(m => new { m.Id, m.StoragePath, m.Description, m.Hash })
                .ToListAsync();

            var request = _httpContextAccessor.HttpContext?.Request;
            return rows.ConvertAll(m => new MediaFileResponseDto
            {
                Id = m.Id,
                StoragePath = m.StoragePath,
                PublicUrl = ToPublicUrl(request, m.StoragePath),
                Description = m.Description,
                Hash = m.Hash
            });
        }

        public async Task<MediaFileResponseDto?> GetByIdAsync(Guid id)
        {
            var file = await _repository.GetByIdAsync(id);
            if (file == null) return null;

            var request = _httpContextAccessor.HttpContext?.Request;
            return new MediaFileResponseDto
            {
                Id = file.Id,
                StoragePath = file.StoragePath,
                PublicUrl = ToPublicUrl(request, file.StoragePath),
                Description = file.Description,
                Hash = file.Hash
            };
        }

        public async Task<MediaFileUploadResultDto> UploadAsync(HttpRequest request, IFormFile file, string? description)
        {
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(ext) || !AllowedMimeTypes.Contains(file.ContentType))
                throw new InvalidOperationException("只接受 JPG 或 PNG 格式的圖片。");

            if (string.IsNullOrWhiteSpace(description))
            {
                var stem = Path.GetFileNameWithoutExtension(file.FileName);
                description = string.IsNullOrWhiteSpace(stem) ? null : stem.Trim();
            }

            string hash;
            using (var sha256 = SHA256.Create())
            {
                using var stream = file.OpenReadStream();
                var bytes = await sha256.ComputeHashAsync(stream);
                hash = Convert.ToHexString(bytes).ToLowerInvariant();
            }

            var existing = await _repository.GetQueryable()
                .FirstOrDefaultAsync(m => m.Hash == hash);
            if (existing != null)
            {
                return new MediaFileUploadResultDto
                {
                    AlreadyExists = true,
                    File = new MediaFileResponseDto
                    {
                        Id = existing.Id,
                        StoragePath = existing.StoragePath,
                        PublicUrl = ToPublicUrl(request, existing.StoragePath),
                        Description = existing.Description,
                        Hash = existing.Hash
                    }
                };
            }

            var uploadFolder = _config["MediaFiles:UploadPath"] ?? "uploads";
            var fullFolder = Path.Combine(_env.ContentRootPath, uploadFolder);
            Directory.CreateDirectory(fullFolder);

            var fileName = hash + ext;
            var storagePath = Path.Combine(uploadFolder, fileName);
            var fullPath = Path.Combine(_env.ContentRootPath, storagePath);

            using (var stream = file.OpenReadStream())
            using (var fs = System.IO.File.Create(fullPath))
            {
                await stream.CopyToAsync(fs);
            }

            var storagePathNormalized = storagePath.Replace('\\', '/');
            var publicUrl = ToPublicUrl(request, storagePathNormalized);

            var mediaFile = new MediaFile
            {
                StoragePath = storagePathNormalized,
                PublicUrl = publicUrl,
                Description = description,
                Hash = hash
            };

            await _repository.InsertAsync(mediaFile);
            await _unitOfWork.SaveChangesAsync();

            return new MediaFileUploadResultDto
            {
                AlreadyExists = false,
                File = new MediaFileResponseDto
                {
                    Id = mediaFile.Id,
                    StoragePath = mediaFile.StoragePath,
                    PublicUrl = mediaFile.PublicUrl,
                    Description = mediaFile.Description,
                    Hash = mediaFile.Hash
                }
            };
        }

        public async Task<bool> UpdateDescriptionAsync(Guid id, UpdateMediaFileDescriptionRequestDto request)
        {
            var file = await _repository.GetByIdAsync(id);
            if (file == null) return false;

            file.Description = request.Description;
            await _repository.UpdateAsync(file);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var file = await _repository.GetByIdAsync(id);
            if (file == null) return false;

            await _repository.DeleteAsync(id);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }
    }
}
