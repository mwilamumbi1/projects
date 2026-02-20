using System.ComponentModel.DataAnnotations;

namespace Core.HumanResourceManagementApi.Models
{
    public class KeyIndicator
    {
        [Key]
        public int IndicatorID { get; set; }

        [Required]
        public string IndicatorName { get; set; } = null!;

        public string? Description { get; set; }
    }

}
