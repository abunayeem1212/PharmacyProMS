using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PharmacyProMS.Models
{
    public class SupplierPayment
    {
        [Key]
        public int PaymentId { get; set; }

        [Required]
        
        [Display(Name = "Payment Amount")]
        public decimal Amount { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Payment Date")]
        public DateTime PaymentDate { get; set; }
            = DateTime.Today;

        [StringLength(50)]
        [Display(Name = "Payment Method")]
        public string PaymentMethod { get; set; }
            = "Cash";

        [StringLength(500)]
        [Display(Name = "Note")]
        public string Note { get; set; }

        public DateTime CreatedAt { get; set; }
            = DateTime.Now;

        public string CreatedBy { get; set; }

        // Foreign Key
        [Required]
        public int SupplierId { get; set; }

        public int? PurchaseId { get; set; }

        // Navigation
        public virtual Supplier Supplier { get; set; }
        public virtual PurchaseInvoice PurchaseInvoice
        { get; set; }
    }
}