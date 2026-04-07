using landing_page_backend;
using landing_page_backend.DTOs;
using landing_page_backend.Repositories;
using landing_page_backend.Services;
using Microsoft.EntityFrameworkCore;

namespace landing_page_backend.Tests.Services
{
    public class SitePageServiceTests
    {
        private MyDbContext CreateInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<MyDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new MyDbContext(options);
        }

        [Fact]
        public async Task GetAllAsync_WithNoFilters_ReturnsAllPages()
        {
            using var context = CreateInMemoryContext();
            context.SitePages.AddRange(
                new SitePage { Id = Guid.NewGuid(), ContentJson = "{\"title\":\"Home\"}" },
                new SitePage { Id = Guid.NewGuid(), ContentJson = "{\"title\":\"About\"}" }
            );
            await context.SaveChangesAsync();

            var service = new SitePageService(new UnitOfWork(context));
            var query = new SitePageQueryDto();

            var result = await service.GetAllAsync(query);

            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task GetAllAsync_WithKeyword_ReturnsFilteredPages()
        {
            using var context = CreateInMemoryContext();
            context.SitePages.AddRange(
                new SitePage { Id = Guid.NewGuid(), ContentJson = "{\"title\":\"Home\",\"hero\":\"Welcome\"}" },
                new SitePage { Id = Guid.NewGuid(), ContentJson = "{\"title\":\"About\",\"content\":\"Info\"}" },
                new SitePage { Id = Guid.NewGuid(), ContentJson = "{\"title\":\"Hero Page\",\"hero\":\"Banner\"}" }
            );
            await context.SaveChangesAsync();

            var service = new SitePageService(new UnitOfWork(context));
            var query = new SitePageQueryDto { Keyword = "hero" };

            var result = await service.GetAllAsync(query);

            Assert.Equal(2, result.Count);
            Assert.All(result, p => Assert.Contains("hero", p.ContentJson, StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public async Task GetAllAsync_WithLimit_ReturnsLimitedResults()
        {
            using var context = CreateInMemoryContext();
            for (int i = 1; i <= 10; i++)
            {
                context.SitePages.Add(new SitePage { Id = Guid.NewGuid(), ContentJson = $"{{\"page\":{i}}}" });
            }
            await context.SaveChangesAsync();

            var service = new SitePageService(new UnitOfWork(context));
            var query = new SitePageQueryDto { Limit = 5 };

            var result = await service.GetAllAsync(query);

            Assert.Equal(5, result.Count);
        }

        [Fact]
        public async Task GetByIdAsync_ExistingPage_ReturnsPage()
        {
            using var context = CreateInMemoryContext();
            var pageId = Guid.NewGuid();
            context.SitePages.Add(new SitePage { Id = pageId, ContentJson = "{\"title\":\"Test\"}" });
            await context.SaveChangesAsync();

            var service = new SitePageService(new UnitOfWork(context));

            var result = await service.GetByIdAsync(pageId);

            Assert.NotNull(result);
            Assert.Equal(pageId, result.Id);
            Assert.Equal("{\"title\":\"Test\"}", result.ContentJson);
        }

        [Fact]
        public async Task GetByIdAsync_NonExistingPage_ReturnsNull()
        {
            using var context = CreateInMemoryContext();
            var service = new SitePageService(new UnitOfWork(context));

            var result = await service.GetByIdAsync(Guid.NewGuid());

            Assert.Null(result);
        }

        [Fact]
        public async Task CreateAsync_ValidPage_CreatesAndReturnsPage()
        {
            using var context = CreateInMemoryContext();
            var service = new SitePageService(new UnitOfWork(context));
            var request = new CreateSitePageRequestDto { ContentJson = "{\"title\":\"New Page\"}" };

            var result = await service.CreateAsync(request);

            Assert.NotNull(result);
            Assert.NotEqual(Guid.Empty, result.Id);
            Assert.Equal("{\"title\":\"New Page\"}", result.ContentJson);

            var dbPage = await context.SitePages.FindAsync(result.Id);
            Assert.NotNull(dbPage);
            Assert.Equal("{\"title\":\"New Page\"}", dbPage.ContentJson);
        }

        [Fact]
        public async Task UpdateAsync_ExistingPage_UpdatesAndReturnsTrue()
        {
            using var context = CreateInMemoryContext();
            var pageId = Guid.NewGuid();
            context.SitePages.Add(new SitePage { Id = pageId, ContentJson = "{\"title\":\"Old\"}" });
            await context.SaveChangesAsync();

            var service = new SitePageService(new UnitOfWork(context));
            var request = new UpdateSitePageRequestDto { ContentJson = "{\"title\":\"Updated\"}" };

            var result = await service.UpdateAsync(pageId, request);

            Assert.True(result);

            var dbPage = await context.SitePages.FindAsync(pageId);
            Assert.NotNull(dbPage);
            Assert.Equal("{\"title\":\"Updated\"}", dbPage.ContentJson);
        }

        [Fact]
        public async Task UpdateAsync_NonExistingPage_ReturnsFalse()
        {
            using var context = CreateInMemoryContext();
            var service = new SitePageService(new UnitOfWork(context));
            var request = new UpdateSitePageRequestDto { ContentJson = "{\"title\":\"Updated\"}" };

            var result = await service.UpdateAsync(Guid.NewGuid(), request);

            Assert.False(result);
        }

        [Fact]
        public async Task DeleteAsync_ExistingPage_DeletesAndReturnsTrue()
        {
            using var context = CreateInMemoryContext();
            var pageId = Guid.NewGuid();
            context.SitePages.Add(new SitePage { Id = pageId, ContentJson = "{\"title\":\"Test\"}" });
            await context.SaveChangesAsync();

            var service = new SitePageService(new UnitOfWork(context));

            var result = await service.DeleteAsync(pageId);

            Assert.True(result);

            var dbPage = await context.SitePages.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id == pageId);
            Assert.NotNull(dbPage);
            Assert.True(dbPage.IsDeleted);
            Assert.NotNull(dbPage.DeletedAt);
        }

        [Fact]
        public async Task DeleteAsync_NonExistingPage_ReturnsFalse()
        {
            using var context = CreateInMemoryContext();
            var service = new SitePageService(new UnitOfWork(context));

            var result = await service.DeleteAsync(Guid.NewGuid());

            Assert.False(result);
        }

        [Fact]
        public async Task GetCurrentPublishedAsync_WhenNonePublished_ReturnsNull()
        {
            using var context = CreateInMemoryContext();
            context.SitePages.Add(new SitePage
            {
                ContentJson = "{}",
                Name = "Draft",
                IsPublished = false
            });
            await context.SaveChangesAsync();

            var service = new SitePageService(new UnitOfWork(context));

            var result = await service.GetCurrentPublishedAsync();

            Assert.Null(result);
        }

        [Fact]
        public async Task GetCurrentPublishedAsync_WhenOnePublished_ReturnsThatPage()
        {
            using var context = CreateInMemoryContext();
            var publishedId = Guid.NewGuid();
            context.SitePages.AddRange(
                new SitePage
                {
                    Id = publishedId,
                    ContentJson = "{\"page\":\"live\"}",
                    Name = "Live",
                    IsPublished = true,
                    CreatedAt = DateTime.UtcNow.AddHours(-1)
                },
                new SitePage
                {
                    ContentJson = "{}",
                    Name = "Draft",
                    IsPublished = false
                });
            await context.SaveChangesAsync();

            var service = new SitePageService(new UnitOfWork(context));

            var result = await service.GetCurrentPublishedAsync();

            Assert.NotNull(result);
            Assert.Equal(publishedId, result.Id);
            Assert.Equal("{\"page\":\"live\"}", result.ContentJson);
            Assert.True(result.IsPublished);
        }

        [Fact]
        public async Task GetCurrentPublishedAsync_WhenMultiplePublished_ReturnsNewestCreatedAt()
        {
            using var context = CreateInMemoryContext();
            var olderId = Guid.NewGuid();
            var newerId = Guid.NewGuid();
            context.SitePages.Add(new SitePage
            {
                Id = olderId,
                ContentJson = "{\"v\":1}",
                Name = "Older",
                IsPublished = true
            });
            await context.SaveChangesAsync();
            await Task.Delay(50);
            context.SitePages.Add(new SitePage
            {
                Id = newerId,
                ContentJson = "{\"v\":2}",
                Name = "Newer",
                IsPublished = true
            });
            await context.SaveChangesAsync();

            var service = new SitePageService(new UnitOfWork(context));

            var result = await service.GetCurrentPublishedAsync();

            Assert.NotNull(result);
            Assert.Equal(newerId, result.Id);
            Assert.Equal("{\"v\":2}", result.ContentJson);
        }

        [Fact]
        public async Task PublishAsync_WhenPageNotFound_ReturnsFalse()
        {
            using var context = CreateInMemoryContext();
            var service = new SitePageService(new UnitOfWork(context));

            var result = await service.PublishAsync(Guid.NewGuid(), isPublished: true);

            Assert.False(result);
        }

        [Fact]
        public async Task PublishAsync_WhenUnpublish_UpdatesIsPublished()
        {
            using var context = CreateInMemoryContext();
            var pageId = Guid.NewGuid();
            context.SitePages.Add(new SitePage
            {
                Id = pageId,
                ContentJson = "{}",
                Name = "Was Live",
                IsPublished = true
            });
            await context.SaveChangesAsync();

            var service = new SitePageService(new UnitOfWork(context));

            var result = await service.PublishAsync(pageId, isPublished: false);

            Assert.True(result);
            var dbPage = await context.SitePages.FindAsync(pageId);
            Assert.NotNull(dbPage);
            Assert.False(dbPage.IsPublished);
        }
    }
}
