using System.ComponentModel.DataAnnotations;

namespace Core.HumanResourceManagementApi.Models
{
    public class ReviewIndicator
    {
        [Key]
        public int ReviewID { get; set; }

        public int IndicatorID { get; set; }
        public int Score { get; set; }
        public string? Comments { get; set; }
    }
}
