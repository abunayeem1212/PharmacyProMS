using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PharmacyProMS.Models
{
    public class SaleInvoiceItem
    {
        [Key]
        public int ItemId { get; set; }

        public int Quantity { get; set; }

       
        [Display(Name = "Sale Price")]
        public decimal SalePrice { get; set; }

       
        [Display(Name = "Purchase Price")]
        public decimal PurchasePrice { get; set; }

        
        [Display(Name = "Sub Total")]
        public decimal SubTotal { get; set; }

        // Foreign Keys
        public int InvoiceId { get; set; }
        public int MedicineId { get; set; }
        public int? BatchId { get; set; }



        [Display(Name = "Item Discount")]
        public decimal ItemDiscount { get; set; } = 0;

        // Navigation
        public virtual SaleInvoice SaleInvoice { get; set; }
        public virtual Medicine Medicine { get; set; }
        public virtual MedicineBatch Batch { get; set; }
    }
}