using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PharmacyProMS.Models
{
    public class PharmacySetting
    {
        [Key]
        public int SettingId { get; set; }

        [Required]
        [Display(Name = "Pharmacy Name")]
        [StringLength(150)]
        public string PharmacyName { get; set; }

        [Display(Name = "Logo")]
        public string LogoPath { get; set; }

        [StringLength(250)]
        public string Address { get; set; }

        [Phone]
        public string Phone { get; set; }

       
        [Display(Name = "VAT %")]
        public decimal VatPercentage { get; set; } = 0;

        [StringLength(10)]
        public string Currency { get; set; } = "BDT";

        [Display(Name = "Low Stock Threshold")]
        public int LowStockThreshold { get; set; } = 10;
    }
}