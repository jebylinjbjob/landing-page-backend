using System.ComponentModel.DataAnnotations;

namespace landing_page_backend
{
    public class SitePage : AuditableEntity
    {
        [Required]
        public string ContentJson { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public bool IsPublished { get; set; } = false;
    }
}
