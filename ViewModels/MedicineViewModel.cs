using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web;
using PharmacyProMS.Models;

namespace PharmacyProMS.ViewModels
{
    public class MedicineViewModel
    {
        public int MedicineId { get; set; }

        [Required(ErrorMessage = "Medicine Name is required")]
        [StringLength(150)]
        [Display(Name = "Medicine Name")]
        public string MedicineName { get; set; }

        [StringLength(150)]
        [Display(Name = "Generic Name")]
        public string GenericName { get; set; }

        [Required(ErrorMessage = "Unit Type is required")]
        [Display(Name = "Unit Type")]
        public string UnitType { get; set; }

        [Required(ErrorMessage = "Sale Price is required")]
        [Range(0.01, 999999,
            ErrorMessage = "Price must be greater than 0")]
        [Display(Name = "Sale Price (৳)")]
        public decimal SalePrice { get; set; }

        [Range(0, 9999,
            ErrorMessage = "Invalid re-order level")]
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

        [Display(Name = "Upload Image")]
        public HttpPostedFileBase ImageFile { get; set; }

        [Required(ErrorMessage = "Company is required")]
        [Display(Name = "Company")]
        public int CompanyId { get; set; }

        [Required(ErrorMessage = "Category is required")]
        [Display(Name = "Category")]
        public int CategoryId { get; set; }

        // Dropdowns
        public IEnumerable<Company> Companies { get; set; }
        public IEnumerable<MedicineCategory> Categories { get; set; }
    }

    public class MedicineListViewModel
    {
        public IEnumerable<Medicine> Medicines { get; set; }
        public string SearchTerm { get; set; }
        public string SortBy { get; set; }
        public string SortOrder { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int TotalCount { get; set; }
        public int PageSize { get; set; } = 10;
        public int? FilterCompany { get; set; }
        public int? FilterCategory { get; set; }
        public string FilterStock { get; set; }

        // Filter Dropdowns
        public IEnumerable<Company> Companies { get; set; }
        public IEnumerable<MedicineCategory> Categories { get; set; }
    }
}