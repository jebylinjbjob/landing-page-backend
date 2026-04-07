using landing_page_backend.DTOs;
using landing_page_backend.Services;

using Microsoft.AspNetCore.Mvc;

namespace landing_page_backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SitePageController : ControllerBase
    {
        private readonly ISitePageService _sitePageService;
        private readonly ILogger<SitePageController> _logger;

        public SitePageController(ISitePageService sitePageService, ILogger<SitePageController> logger)
        {
            _sitePageService = sitePageService;
            _logger = logger;
        }

        /// <summary>
        /// 取得所有頁面
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<SitePageResponseDto>>> GetAll([FromQuery] SitePageQueryDto query)
        {
            var pages = await _sitePageService.GetAllAsync(query);
            return Ok(pages);
        }

        /// <summary>
        /// 取得單一頁面
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<SitePageResponseDto>> GetById(Guid id)
        {
            var page = await _sitePageService.GetByIdAsync(id);
            if (page == null)
            {
                _logger.LogWarning("SitePage {Id} not found", id);
                return NotFound();
            }
            return Ok(page);
        }

        /// <summary>
        /// 新增頁面
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<ActionResult<SitePageResponseDto>> Create([FromBody] CreateSitePageRequestDto request)
        {
            var page = await _sitePageService.CreateAsync(request);
            _logger.LogInformation("SitePage created: {Id}", page.Id);
            return CreatedAtAction(nameof(GetById), new { id = page.Id }, page);
        }

        /// <summary>
        /// 更新頁面 JSON 內容
        /// </summary>
        /// <param name="id"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSitePageRequestDto request)
        {
            var updated = await _sitePageService.UpdateAsync(id, request);
            if (!updated)
            {
                _logger.LogWarning("Update: SitePage {Id} not found", id);
                return NotFound();
            }
            _logger.LogInformation("SitePage {Id} updated", id);
            return NoContent();
        }

        /// <summary>
        /// 刪除頁面
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var deleted = await _sitePageService.DeleteAsync(id);
            if (!deleted)
            {
                _logger.LogWarning("Delete: SitePage {Id} not found", id);
                return NotFound();
            }
            _logger.LogInformation("SitePage {Id} deleted", id);
            return NoContent();
        }

        /// <summary>
        /// 發布頁面
        /// </summary>
        /// <param name="id"></param>
        /// <param name="isPublished"></param>
        /// <returns></returns>
        [HttpPut("{id:guid}/publish")]
        public async Task<IActionResult> Publish(Guid id, [FromQuery] bool isPublished = true)
        {
            var Result = await _sitePageService.PublishAsync(id, isPublished);
            if (!Result)
            {
                _logger.LogWarning("Publish: SitePage {Id} not found", id);
                return NotFound();
            }
            _logger.LogInformation("SitePage {Id} publish state set to {IsPublished}", id, isPublished);
            return NoContent();
        }
        /// <summary>
        /// 取得發布內容
        /// </summary>
        /// <returns></returns>
        [HttpGet("GetLandingPage")]
        public async Task<IActionResult> GetLandingPage()
        {
            var landingPage = await _sitePageService.GetCurrentPublishedAsync();
            if (landingPage == null)
            {
                return NotFound();
            }
            return Ok(landingPage.ContentJson);
        }
    }
}
