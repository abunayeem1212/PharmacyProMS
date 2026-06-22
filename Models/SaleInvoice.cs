using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PharmacyProMS.Models
{
    public class SaleInvoice
    {
        [Key]
        public int InvoiceId { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Invoice Number")]
        public string InvoiceNumber { get; set; }

        [Required]
        [Display(Name = "Sale Date")]
        [DataType(DataType.Date)]
        public DateTime SaleDate { get; set; } = DateTime.Today;

        [StringLength(100)]
        [Display(Name = "Customer Name")]
        public string WalkInCustomerName { get; set; }

        
        public decimal TotalAmount { get; set; }

       
        public decimal Discount { get; set; } = 0;

        
        [Display(Name = "VAT Amount")]
        public decimal VatAmount { get; set; } = 0;

        
        [Display(Name = "Net Amount")]
        public decimal NetAmount { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Foreign Keys
        public int? CustomerId { get; set; }
        public int? PrescriptionId { get; set; }
        public string CreatedBy { get; set; }

        // Navigation
        public virtual Customer Customer { get; set; }
        public virtual Prescription Prescription { get; set; }
        public virtual ICollection<SaleInvoiceItem> SaleItems { get; set; }



        [Display(Name = "Is Credit Sale")]
        public bool IsCreditSale { get; set; } = false;

        
        [Display(Name = "Paid Amount")]
        public decimal PaidAmount { get; set; } = 0;

        [Display(Name = "Due Amount")]
        public decimal DueAmount { get; set; } = 0;

        [StringLength(50)]
        [Display(Name = "Payment Method")]
        public string PaymentMethod { get; set; } = "Cash";

        [StringLength(500)]
        [Display(Name = "Note")]
        public string Note { get; set; }


        // ← এই fields add করো
        [StringLength(20)]
        public string InvoiceStatus { get; set; } = "Active";
        // Active, Cancelled, Void

        public DateTime? CancelledAt { get; set; }
        public string CancelledBy { get; set; }
        public string CancelReason { get; set; }

    }
}