using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PharmacyProMS.Models
{
    public class PurchaseInvoice
    {
        [Key]
        public int PurchaseId { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Invoice Number")]
        public string InvoiceNumber { get; set; }

        [Required]
        [Display(Name = "Purchase Date")]
        [DataType(DataType.Date)]
        public DateTime PurchaseDate { get; set; } = DateTime.Today;

        
        [Display(Name = "Total Amount")]
        public decimal TotalAmount { get; set; }

        
        [Display(Name = "Discount")]
        public decimal Discount { get; set; } = 0;

        
        [Display(Name = "Net Amount")]
        public decimal NetAmount { get; set; }

        
        [Display(Name = "Paid Amount")]
        public decimal PaidAmount { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Foreign Keys
        [Required(ErrorMessage = "Supplier is required")]
        [Display(Name = "Supplier")]
        public int SupplierId { get; set; }

        public string CreatedBy { get; set; }

        // Navigation
        public virtual Supplier Supplier { get; set; }
        public virtual ICollection<PurchaseInvoiceItem> PurchaseItems { get; set; }


        [StringLength(500)]
        [Display(Name = "Note")]
        public string Note { get; set; }

        [StringLength(20)]
        public string InvoiceStatus { get; set; } = "Active";
        public DateTime? CancelledAt { get; set; }
        public string CancelledBy { get; set; }
        public string CancelReason { get; set; }



    }
}