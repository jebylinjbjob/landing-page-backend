using System.ComponentModel.DataAnnotations;

namespace landing_page_backend.DTOs
{
    public class MediaFileQueryDto
    {
        public string? Keyword { get; set; }
        public string? Hash { get; set; }
        public int? Limit { get; set; }
    }

    public class MediaFileUploadResultDto
    {
        public MediaFileResponseDto File { get; set; } = new();
        public bool AlreadyExists { get; set; }
    }

    public class MediaFileResponseDto
    {
        public Guid Id { get; set; }
        public string StoragePath { get; set; } = string.Empty;
        public string PublicUrl { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Hash { get; set; } = string.Empty;
    }

    public class UpdateMediaFileDescriptionRequestDto
    {
        [Required]
        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;
    }
}
