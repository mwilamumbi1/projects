using System.ComponentModel.DataAnnotations;
namespace Core.HumanResourceManagementApi.Models

{


    public class Contract
    {
        [Key]
        public int ContractID { get; set; }

        [Required]
        public string ContractType { get; set; } = null!;

        public string? Description { get; set; }
    }
}
