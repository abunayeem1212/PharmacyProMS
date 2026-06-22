using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PharmacyProMS.Models
{
    public class PurchaseReturn
    {
        [Key]
        public int PReturnId { get; set; }

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
        [Display(Name = "Purchase Invoice")]
        public int PurchaseId { get; set; }

        // Navigation
        public virtual PurchaseInvoice PurchaseInvoice
        { get; set; }
        public virtual ICollection<PurchaseReturnItem>
            ReturnItems
        { get; set; }
    }

    public class PurchaseReturnItem
    {
        [Key]
        public int PReturnItemId { get; set; }

        public int Quantity { get; set; }

        
        public decimal ReturnAmount { get; set; }

        // Foreign Keys
        public int PReturnId { get; set; }
        public int MedicineId { get; set; }

        // Navigation
        public virtual PurchaseReturn PurchaseReturn
        { get; set; }
        public virtual Medicine Medicine { get; set; }
    }
}