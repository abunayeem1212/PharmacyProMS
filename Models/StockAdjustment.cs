using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PharmacyProMS.Models
{
    public class StockAdjustment
    {
        [Key]
        public int AdjustmentId { get; set; }

        [Required]
        [Display(Name = "Adjustment Type")]
        [StringLength(20)]
        public string AdjustmentType { get; set; }
        // "Increase" or "Decrease"

        [Required]
        [Display(Name = "Quantity")]
        public int Quantity { get; set; }

        [Required]
        [Display(Name = "Reason")]
        [StringLength(50)]
        public string Reason { get; set; }
        // Damage, Lost, Expired, Found, Correction

        [StringLength(500)]
        [Display(Name = "Note")]
        public string Note { get; set; }

        [Display(Name = "Stock Before")]
        public int StockBefore { get; set; }

        [Display(Name = "Stock After")]
        public int StockAfter { get; set; }

        public DateTime CreatedAt { get; set; }
            = DateTime.Now;

        public string CreatedBy { get; set; }

        // Foreign Key
        [Required]
        public int MedicineId { get; set; }

        // Navigation
        public virtual Medicine Medicine { get; set; }
    }
}