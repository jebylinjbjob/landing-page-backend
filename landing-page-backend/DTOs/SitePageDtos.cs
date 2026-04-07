using System.ComponentModel.DataAnnotations;

namespace landing_page_backend.DTOs
{
    public class SitePageQueryDto
    {
        public string? Keyword { get; set; }
        public int? Limit { get; set; }
    }

    public class SitePageResponseDto
    {
        public Guid Id { get; set; }
        public string ContentJson { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool IsPublished { get; set; }
    }

    public class CreateSitePageRequestDto
    {
        [Required]
        [MinLength(2)]
        public string ContentJson { get; set; } = string.Empty;
        [Required]
        [MinLength(2)]
        public string Name { get; set; } = string.Empty;
    }

    public class UpdateSitePageRequestDto
    {
        [Required]
        [MinLength(2)]
        public string ContentJson { get; set; } = string.Empty;
        [Required]
        [MinLength(2)]
        public string Name { get; set; } = string.Empty;
    }
}
