using System.ComponentModel.DataAnnotations;

namespace landing_page_backend
{
    public class MediaFile : AuditableEntity
    {
        [Required]
        [MaxLength(500)]
        public string StoragePath { get; set; } = string.Empty;

        [Required]
        [MaxLength(1000)]
        public string PublicUrl { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        [Required]
        [MaxLength(64)]
        public string Hash { get; set; } = string.Empty;

    }
}
