using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyPhongTro.Models
{
    public class UtilityReading
    {
        public int Id { get; set; }

        [Required]
        public int RoomId { get; set; }
        public Room? Room { get; set; }

        [Required, DataType(DataType.Date)]
        public DateTime BillingMonth { get; set; }

        [Range(0, double.MaxValue)]
        [Column(TypeName = "decimal(18,2)")]
        public decimal ElectricityUsage { get; set; }

        [Range(0, double.MaxValue)]
        [Column(TypeName = "decimal(18,2)")]
        public decimal WaterUsage { get; set; }

        [MaxLength(500)]
        public string? Note { get; set; }

        public int? ContractId { get; set; }
        public Contract? Contract { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal? PreviousElectricityMeter { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal? CurrentElectricityMeter { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal? ElectricityUnitPrice { get; set; }
        [ConcurrencyCheck]
        public DateTime? ElectricityPaidAt { get; set; }
        [NotMapped]
        public decimal? ElectricityAmount => ElectricityUnitPrice.HasValue ? ElectricityUsage * ElectricityUnitPrice.Value : null;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        [ConcurrencyCheck]
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}
