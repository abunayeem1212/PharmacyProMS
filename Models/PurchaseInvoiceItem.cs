using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PharmacyProMS.Models
{
    public class PurchaseInvoiceItem
    {
        [Key]
        public int ItemId { get; set; }

        [Display(Name = "Quantity")]
        public int Quantity { get; set; }

        
        [Display(Name = "Purchase Price")]
        public decimal PurchasePrice { get; set; }

        
        [Display(Name = "Sub Total")]
        public decimal SubTotal { get; set; }

        // Foreign Keys
        public int PurchaseId { get; set; }
        public int MedicineId { get; set; }
        public int? BatchId { get; set; }

        // Navigation
        public virtual PurchaseInvoice PurchaseInvoice { get; set; }
        public virtual Medicine Medicine { get; set; }
        public virtual MedicineBatch Batch { get; set; }
    }
}