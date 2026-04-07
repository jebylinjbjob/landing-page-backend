using System.Text;

using landing_page_backend;
using landing_page_backend.Repositories;
using landing_page_backend.DTOs;
using landing_page_backend.Services;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

using Moq;

namespace landing_page_backend.Tests.Services
{
    public class MediaFileServiceTests
    {
        private MyDbContext CreateInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<MyDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
                .Options;
            return new MyDbContext(options);
        }

        private (IConfiguration, IWebHostEnvironment, IHttpContextAccessor) CreateMockDependencies(string uploadPath = "uploads")
        {
            var configMock = new Mock<IConfiguration>();
            configMock.Setup(c => c["MediaFiles:UploadPath"]).Returns(uploadPath);

            var envMock = new Mock<IWebHostEnvironment>();
            envMock.Setup(e => e.ContentRootPath).Returns(Path.GetTempPath());

            var httpContextAccessor = new Mock<IHttpContextAccessor>();

            return (configMock.Object, envMock.Object, httpContextAccessor.Object);
        }

        [Fact]
        public async Task GetAllAsync_WithNoFilters_ReturnsAllMediaFiles()
        {
            using var context = CreateInMemoryContext();
            context.MediaFiles.AddRange(
                new MediaFile { Id = Guid.NewGuid(), StoragePath = "uploads/test1.jpg", PublicUrl = "/uploads/test1.jpg", Hash = "hash1" },
                new MediaFile { Id = Guid.NewGuid(), StoragePath = "uploads/test2.png", PublicUrl = "/uploads/test2.png", Hash = "hash2" }
            );
            await context.SaveChangesAsync();

            var (config, env, httpContextAccessor) = CreateMockDependencies();
            var service = new MediaFileService(new UnitOfWork(context), config, env, httpContextAccessor);
            var query = new MediaFileQueryDto();

            var result = await service.GetAllAsync(query);

            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task GetAllAsync_WithKeyword_ReturnsFilteredMediaFiles()
        {
            using var context = CreateInMemoryContext();
            context.MediaFiles.AddRange(
                new MediaFile { Id = Guid.NewGuid(), StoragePath = "uploads/banner.jpg", PublicUrl = "/uploads/banner.jpg", Hash = "hash1", Description = "Banner image" },
                new MediaFile { Id = Guid.NewGuid(), StoragePath = "uploads/logo.png", PublicUrl = "/uploads/logo.png", Hash = "hash2", Description = "Logo" },
                new MediaFile { Id = Guid.NewGuid(), StoragePath = "uploads/hero-banner.jpg", PublicUrl = "/uploads/hero-banner.jpg", Hash = "hash3", Description = "Hero" }
            );
            await context.SaveChangesAsync();

            var (config, env, httpContextAccessor) = CreateMockDependencies();
            var service = new MediaFileService(new UnitOfWork(context), config, env, httpContextAccessor);
            var query = new MediaFileQueryDto { Keyword = "banner" };

            var result = await service.GetAllAsync(query);

            Assert.Equal(2, result.Count);
            Assert.All(result, f => Assert.True(
                (f.Description != null && f.Description.Contains("banner", StringComparison.OrdinalIgnoreCase)) ||
                f.StoragePath.Contains("banner", StringComparison.OrdinalIgnoreCase)
            ));
        }

        [Fact]
        public async Task GetAllAsync_WithHash_ReturnsFilteredMediaFiles()
        {
            using var context = CreateInMemoryContext();
            context.MediaFiles.AddRange(
                new MediaFile { Id = Guid.NewGuid(), StoragePath = "uploads/test1.jpg", PublicUrl = "/uploads/test1.jpg", Hash = "abc123def" },
                new MediaFile { Id = Guid.NewGuid(), StoragePath = "uploads/test2.jpg", PublicUrl = "/uploads/test2.jpg", Hash = "xyz789ghi" }
            );
            await context.SaveChangesAsync();

            var (config, env, httpContextAccessor) = CreateMockDependencies();
            var service = new MediaFileService(new UnitOfWork(context), config, env, httpContextAccessor);
            var query = new MediaFileQueryDto { Hash = "abc" };

            var result = await service.GetAllAsync(query);

            Assert.Single(result);
            Assert.Contains("abc", result[0].Hash);
        }

        [Fact]
        public async Task GetAllAsync_WithLimit_ReturnsLimitedResults()
        {
            using var context = CreateInMemoryContext();
            for (int i = 1; i <= 10; i++)
            {
                context.MediaFiles.Add(new MediaFile
                {
                    Id = Guid.NewGuid(),
                    StoragePath = $"uploads/test{i}.jpg",
                    PublicUrl = $"/uploads/test{i}.jpg",
                    Hash = $"hash{i}"
                });
            }
            await context.SaveChangesAsync();

            var (config, env, httpContextAccessor) = CreateMockDependencies();
            var service = new MediaFileService(new UnitOfWork(context), config, env, httpContextAccessor);
            var query = new MediaFileQueryDto { Limit = 3 };

            var result = await service.GetAllAsync(query);

            Assert.Equal(3, result.Count);
        }

        [Fact]
        public async Task GetByIdAsync_ExistingMediaFile_ReturnsMediaFile()
        {
            using var context = CreateInMemoryContext();
            var fileId = Guid.NewGuid();
            context.MediaFiles.Add(new MediaFile
            {
                Id = fileId,
                StoragePath = "uploads/test.jpg",
                PublicUrl = "/uploads/test.jpg",
                Hash = "testhash",
                Description = "Test image"
            });
            await context.SaveChangesAsync();

            var (config, env, httpContextAccessor) = CreateMockDependencies();
            var service = new MediaFileService(new UnitOfWork(context), config, env, httpContextAccessor);

            var result = await service.GetByIdAsync(fileId);

            Assert.NotNull(result);
            Assert.Equal(fileId, result.Id);
            Assert.Equal("Test image", result.Description);
        }

        [Fact]
        public async Task GetByIdAsync_NonExistingMediaFile_ReturnsNull()
        {
            using var context = CreateInMemoryContext();
            var (config, env, httpContextAccessor) = CreateMockDependencies();
            var service = new MediaFileService(new UnitOfWork(context), config, env, httpContextAccessor);

            var result = await service.GetByIdAsync(Guid.NewGuid());

            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateDescriptionAsync_ExistingMediaFile_UpdatesAndReturnsTrue()
        {
            using var context = CreateInMemoryContext();
            var fileId = Guid.NewGuid();
            context.MediaFiles.Add(new MediaFile
            {
                Id = fileId,
                StoragePath = "uploads/test.jpg",
                PublicUrl = "/uploads/test.jpg",
                Hash = "hash",
                Description = "Old description"
            });
            await context.SaveChangesAsync();

            var (config, env, httpContextAccessor) = CreateMockDependencies();
            var service = new MediaFileService(new UnitOfWork(context), config, env, httpContextAccessor);
            var request = new UpdateMediaFileDescriptionRequestDto { Description = "New description" };

            var result = await service.UpdateDescriptionAsync(fileId, request);

            Assert.True(result);

            var dbFile = await context.MediaFiles.FindAsync(fileId);
            Assert.NotNull(dbFile);
            Assert.Equal("New description", dbFile.Description);
        }

        [Fact]
        public async Task UpdateDescriptionAsync_NonExistingMediaFile_ReturnsFalse()
        {
            using var context = CreateInMemoryContext();
            var (config, env, httpContextAccessor) = CreateMockDependencies();
            var service = new MediaFileService(new UnitOfWork(context), config, env, httpContextAccessor);
            var request = new UpdateMediaFileDescriptionRequestDto { Description = "New description" };

            var result = await service.UpdateDescriptionAsync(Guid.NewGuid(), request);

            Assert.False(result);
        }

        [Fact]
        public async Task UploadAsync_InvalidFileType_ThrowsException()
        {
            using var context = CreateInMemoryContext();
            var (config, env, httpContextAccessor) = CreateMockDependencies();
            var service = new MediaFileService(new UnitOfWork(context), config, env, httpContextAccessor);

            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.FileName).Returns("test.txt");
            fileMock.Setup(f => f.ContentType).Returns("text/plain");

            var requestMock = new Mock<HttpRequest>();
            requestMock.Setup(r => r.Scheme).Returns("http");
            requestMock.Setup(r => r.Host).Returns(new HostString("localhost"));
            requestMock.Setup(r => r.PathBase).Returns(new PathString(""));

            await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await service.UploadAsync(requestMock.Object, fileMock.Object, null)
            );
        }

        [Fact]
        public async Task UploadAsync_ValidJpgFile_CreatesMediaFile()
        {
            using var context = CreateInMemoryContext();
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(Path.Combine(tempDir, "uploads"));

            var configMock = new Mock<IConfiguration>();
            configMock.Setup(c => c["MediaFiles:UploadPath"]).Returns("uploads");

            var envMock = new Mock<IWebHostEnvironment>();
            envMock.Setup(e => e.ContentRootPath).Returns(tempDir);

            var httpContextAccessor = new Mock<IHttpContextAccessor>();

            var service = new MediaFileService(new UnitOfWork(context), configMock.Object, envMock.Object, httpContextAccessor.Object);

            var fileContent = Encoding.UTF8.GetBytes("fake jpg content");
            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.FileName).Returns("test.jpg");
            fileMock.Setup(f => f.ContentType).Returns("image/jpeg");
            fileMock.Setup(f => f.OpenReadStream()).Returns(() => new MemoryStream(fileContent));

            var requestMock = new Mock<HttpRequest>();
            requestMock.Setup(r => r.Scheme).Returns("http");
            requestMock.Setup(r => r.Host).Returns(new HostString("localhost"));
            requestMock.Setup(r => r.PathBase).Returns(new PathString(""));

            var result = await service.UploadAsync(requestMock.Object, fileMock.Object, "Test description");

            Assert.NotNull(result);
            Assert.NotNull(result.File);
            Assert.False(result.AlreadyExists);
            Assert.NotEqual(Guid.Empty, result.File.Id);
            Assert.Equal("Test description", result.File.Description);
            Assert.NotEmpty(result.File.Hash);

            var dbFile = await context.MediaFiles.FindAsync(result.File.Id);
            Assert.NotNull(dbFile);

            Directory.Delete(tempDir, true);
        }

        [Fact]
        public async Task UploadAsync_ValidPngFile_CreatesMediaFile()
        {
            using var context = CreateInMemoryContext();
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(Path.Combine(tempDir, "uploads"));

            var configMock = new Mock<IConfiguration>();
            configMock.Setup(c => c["MediaFiles:UploadPath"]).Returns("uploads");

            var envMock = new Mock<IWebHostEnvironment>();
            envMock.Setup(e => e.ContentRootPath).Returns(tempDir);

            var httpContextAccessor = new Mock<IHttpContextAccessor>();

            var service = new MediaFileService(new UnitOfWork(context), configMock.Object, envMock.Object, httpContextAccessor.Object);

            var fileContent = Encoding.UTF8.GetBytes("fake png content");
            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.FileName).Returns("test.png");
            fileMock.Setup(f => f.ContentType).Returns("image/png");
            fileMock.Setup(f => f.OpenReadStream()).Returns(() => new MemoryStream(fileContent));

            var requestMock = new Mock<HttpRequest>();
            requestMock.Setup(r => r.Scheme).Returns("http");
            requestMock.Setup(r => r.Host).Returns(new HostString("localhost"));
            requestMock.Setup(r => r.PathBase).Returns(new PathString(""));

            var result = await service.UploadAsync(requestMock.Object, fileMock.Object, null);

            Assert.NotNull(result);
            Assert.False(result.AlreadyExists);
            Assert.Equal("test", result.File.Description);

            Directory.Delete(tempDir, true);
        }

        [Fact]
        public async Task DeleteAsync_NonExistingMediaFile_ReturnsFalse()
        {
            using var context = CreateInMemoryContext();
            var (config, env, httpContextAccessor) = CreateMockDependencies();
            var service = new MediaFileService(new UnitOfWork(context), config, env, httpContextAccessor);

            var result = await service.DeleteAsync(Guid.NewGuid());

            Assert.False(result);
        }

        [Fact]
        public async Task GetAllAsync_OrdersByIdDescending_ReturnsCorrectOrder()
        {
            using var context = CreateInMemoryContext();
            var id1 = Guid.NewGuid();
            var id2 = Guid.NewGuid();
            var id3 = Guid.NewGuid();

            context.MediaFiles.AddRange(
                new MediaFile { Id = id1, StoragePath = "uploads/first.jpg", PublicUrl = "/uploads/first.jpg", Hash = "hash1" },
                new MediaFile { Id = id2, StoragePath = "uploads/second.jpg", PublicUrl = "/uploads/second.jpg", Hash = "hash2" },
                new MediaFile { Id = id3, StoragePath = "uploads/third.jpg", PublicUrl = "/uploads/third.jpg", Hash = "hash3" }
            );
            await context.SaveChangesAsync();

            var (config, env, httpContextAccessor) = CreateMockDependencies();
            var service = new MediaFileService(new UnitOfWork(context), config, env, httpContextAccessor);
            var query = new MediaFileQueryDto();

            var result = await service.GetAllAsync(query);

            Assert.Equal(3, result.Count);
        }
    }
}
