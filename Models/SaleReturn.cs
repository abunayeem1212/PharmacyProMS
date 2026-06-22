using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PharmacyProMS.Models
{
    public class SaleReturn
    {
        [Key]
        public int ReturnId { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Return Number")]
        public string ReturnNumber { get; set; }

        [Required]
        [Display(Name = "Return Date")]
        [DataType(DataType.Date)]
        public DateTime ReturnDate { get; set; }
            = DateTime.Today;

        [StringLength(500)]
        [Display(Name = "Reason")]
        public string Reason { get; set; }

        
        [Display(Name = "Total Return Amount")]
        public decimal TotalReturnAmount { get; set; }

        public DateTime CreatedAt { get; set; }
            = DateTime.Now;

        public string CreatedBy { get; set; }

        // Foreign Key
        [Required]
        [Display(Name = "Sale Invoice")]
        public int InvoiceId { get; set; }

        // Navigation
        public virtual SaleInvoice SaleInvoice { get; set; }
        public virtual ICollection<SaleReturnItem>
            ReturnItems
        { get; set; }
    }

    public class SaleReturnItem
    {
        [Key]
        public int ReturnItemId { get; set; }

        public int Quantity { get; set; }

        
        public decimal RefundAmount { get; set; }

        // Foreign Keys
        public int ReturnId { get; set; }
        public int MedicineId { get; set; }

        // Navigation
        public virtual SaleReturn SaleReturn { get; set; }
        public virtual Medicine Medicine { get; set; }
    }
}