using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using PharmacyProMS.Models;

namespace PharmacyProMS.ViewModels
{
    public class MedicineBatchViewModel
    {
        public int BatchId { get; set; }

        [Required(ErrorMessage = "Batch Number is required")]
        [StringLength(50)]
        [Display(Name = "Batch Number")]
        public string BatchNumber { get; set; }

        [Required(ErrorMessage = "Purchase Price is required")]
        [Range(0.01, 999999,
            ErrorMessage = "Price must be greater than 0")]
        [Display(Name = "Purchase Price (৳)")]
        public decimal PurchasePrice { get; set; }

        [Display(Name = "Manufacture Date")]
        [DataType(DataType.Date)]
        public DateTime? ManufactureDate { get; set; }

        [Required(ErrorMessage = "Expiry Date is required")]
        [Display(Name = "Expiry Date")]
        [DataType(DataType.Date)]
        public DateTime ExpiryDate { get; set; }

        [Required(ErrorMessage = "Quantity is required")]
        [Range(1, 99999,
            ErrorMessage = "Quantity must be greater than 0")]
        [Display(Name = "Quantity")]
        public int Quantity { get; set; }

        [Required(ErrorMessage = "Medicine is required")]
        [Display(Name = "Medicine")]
        public int MedicineId { get; set; }

        [Display(Name = "Supplier")]
        public int? SupplierId { get; set; }

        // Dropdowns
        public IEnumerable<Medicine> Medicines { get; set; }
        public IEnumerable<Supplier> Suppliers { get; set; }
    }

    public class MedicineBatchListViewModel
    {
        public IEnumerable<MedicineBatch> Batches { get; set; }
        public string SearchTerm { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int TotalCount { get; set; }
        public int PageSize { get; set; } = 10;
        public int? FilterMedicine { get; set; }
        public string FilterStatus { get; set; }
        public IEnumerable<Medicine> Medicines { get; set; }
    }
}