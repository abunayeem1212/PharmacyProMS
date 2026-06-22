using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PharmacyProMS.Models
{
    public class MedicineBatch
    {
        [Key]
        public int BatchId { get; set; }

        [Required(ErrorMessage = "Batch Number is required")]
        [StringLength(50)]
        [Display(Name = "Batch Number")]
        public string BatchNumber { get; set; }

        
        [Display(Name = "Purchase Price")]
        public decimal PurchasePrice { get; set; }

        [Display(Name = "Manufacture Date")]
        [DataType(DataType.Date)]
        public DateTime? ManufactureDate { get; set; }

        [Required(ErrorMessage = "Expiry Date is required")]
        [Display(Name = "Expiry Date")]
        [DataType(DataType.Date)]
        public DateTime ExpiryDate { get; set; }

        [Display(Name = "Quantity")]
        public int Quantity { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Foreign Keys
        [Required]
        public int MedicineId { get; set; }

        public int? SupplierId { get; set; }

        // Navigation
        public virtual Medicine Medicine { get; set; }
        public virtual Supplier Supplier { get; set; }

        public int? PurchaseId { get; set; }
    }
}