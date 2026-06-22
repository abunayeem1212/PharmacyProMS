using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PharmacyProMS.Models
{
    public class Medicine
    {
        public int MedicineId { get; set; }

        [Required(ErrorMessage = "Medicine Name is required")]
        [StringLength(150)]
        [Display(Name = "Medicine Name")]
        public string MedicineName { get; set; }

        [StringLength(150)]
        [Display(Name = "Generic Name")]
        public string GenericName { get; set; }

        [StringLength(50)]
        [Display(Name = "Unit Type")]
        public string UnitType { get; set; } // Tablet, Syrup, Capsule

        
        [Display(Name = "Sale Price")]
        [Required(ErrorMessage = "Sale Price is required")]
        public decimal SalePrice { get; set; }

        [Display(Name = "Re-Order Level")]
        public int ReOrderLevel { get; set; } = 10;

        [Display(Name = "Current Stock")]
        public int CurrentStock { get; set; } = 0;

        [StringLength(100)]
        [Display(Name = "Barcode")]
        public string Barcode { get; set; }

        [Display(Name = "Prescription Required")]
        public bool IsPrescriptionRequired { get; set; } = false;

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Medicine Image")]
        public string ImagePath { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Foreign Keys
        [Required(ErrorMessage = "Company is required")]
        [Display(Name = "Company")]
        public int CompanyId { get; set; }

        [Required(ErrorMessage = "Category is required")]
        [Display(Name = "Category")]
        public int CategoryId { get; set; }

        // Navigation
        public virtual Company Company { get; set; }
        public virtual MedicineCategory Category { get; set; }
        public virtual ICollection<MedicineBatch> Batches { get; set; }
        public virtual ICollection<SaleInvoiceItem> SaleItems { get; set; }
        public virtual ICollection<PurchaseInvoiceItem> PurchaseItems { get; set; }
    }
}