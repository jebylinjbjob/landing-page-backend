using landing_page_backend.DTOs;
using landing_page_backend.Services;

using Microsoft.AspNetCore.Mvc;

namespace landing_page_backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MediaFileController : ControllerBase
    {
        private readonly IMediaFileService _mediaFileService;
        private readonly ILogger<MediaFileController> _logger;

        public MediaFileController(IMediaFileService mediaFileService, ILogger<MediaFileController> logger)
        {
            _mediaFileService = mediaFileService;
            _logger = logger;
        }

        /// <summary>
        /// 取得所有圖片
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<MediaFileResponseDto>>> GetAll([FromQuery] MediaFileQueryDto query)
        {
            var files = await _mediaFileService.GetAllAsync(query);
            return Ok(files);
        }

        /// <summary>
        /// 取得單一圖片
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<MediaFileResponseDto>> GetById(Guid id)
        {
            var file = await _mediaFileService.GetByIdAsync(id);
            if (file == null)
            {
                _logger.LogWarning("MediaFile {Id} not found", id);
                return NotFound();
            }
            return Ok(file);
        }

        /// <summary>
        /// 上傳圖片
        /// </summary>
        /// <param name="file"></param>
        /// <param name="description"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<ActionResult<MediaFileResponseDto>> Upload(
            IFormFile file,
            [FromForm] string? description)
        {
            try
            {
                var result = await _mediaFileService.UploadAsync(Request, file, description);
                _logger.LogInformation("MediaFile uploaded: {Id}, path: {StoragePath}", result.File.Id, result.File.StoragePath);
                if (result.AlreadyExists)
                    return Ok(result.File);
                return CreatedAtAction(nameof(GetById), new { id = result.File.Id }, result.File);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Upload rejected: invalid file type {FileName} ({ContentType})", file.FileName, file.ContentType);
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// 更新描述
        /// </summary>
        /// <param name="id"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPatch("{id:guid}/description")]
        public async Task<IActionResult> UpdateDescription(Guid id, [FromBody] UpdateMediaFileDescriptionRequestDto request)
        {
            var updated = await _mediaFileService.UpdateDescriptionAsync(id, request);
            if (!updated)
            {
                _logger.LogWarning("UpdateDescription: MediaFile {Id} not found", id);
                return NotFound();
            }
            _logger.LogInformation("MediaFile {Id} description updated", id);
            return NoContent();
        }

        /// <summary>
        /// 刪除圖片
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var deleted = await _mediaFileService.DeleteAsync(id);
            if (!deleted)
            {
                _logger.LogWarning("Delete: MediaFile {Id} not found", id);
                return NotFound();
            }
            _logger.LogInformation("MediaFile {Id} deleted", id);
            return NoContent();
        }
    }
}
