using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PharmacyProMS.Models
{
    public class CustomerPayment
    {
        [Key]
        public int PaymentId { get; set; }

        [Required]
        
        [Display(Name = "Payment Amount")]
        public decimal Amount { get; set; }

        [Required]
        [Display(Name = "Payment Date")]
        [DataType(DataType.Date)]
        public DateTime PaymentDate { get; set; }
            = DateTime.Today;

        [StringLength(500)]
        [Display(Name = "Note")]
        public string Note { get; set; }

        [StringLength(50)]
        [Display(Name = "Payment Method")]
        public string PaymentMethod { get; set; }
            = "Cash";

        public DateTime CreatedAt { get; set; }
            = DateTime.Now;

        public string CreatedBy { get; set; }

        // Foreign Keys
        [Required]
        public int CustomerId { get; set; }

        public int? InvoiceId { get; set; }

        // Navigation
        public virtual Customer Customer { get; set; }
        public virtual SaleInvoice SaleInvoice
        { get; set; }
    }
}