using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using PharmacyProMS.Models;

namespace PharmacyProMS.ViewModels
{
    public class SupplierViewModel
    {
        public int SupplierId { get; set; }

        [Required(ErrorMessage = "Supplier Name is required")]
        [StringLength(100, ErrorMessage = "Max 100 characters")]
        [Display(Name = "Supplier Name")]
        public string SupplierName { get; set; }

        [Phone(ErrorMessage = "Invalid phone number")]
        [Display(Name = "Phone")]
        public string Phone { get; set; }

        [StringLength(200)]
        [Display(Name = "Address")]
        public string Address { get; set; }

        [EmailAddress(ErrorMessage = "Invalid email")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Display(Name = "Opening Balance")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal OpeningBalance { get; set; } = 0;

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;

        [Required(ErrorMessage = "Company is required")]
        [Display(Name = "Company")]
        public int CompanyId { get; set; }

        // For Dropdown
        public IEnumerable<Company> Companies { get; set; }
    }

    public class SupplierListViewModel
    {
        public IEnumerable<Supplier> Suppliers { get; set; }
        public string SearchTerm { get; set; }
        public string SortBy { get; set; }
        public string SortOrder { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int TotalCount { get; set; }
        public int PageSize { get; set; } = 10;

        // Filter
        public int? FilterCompanyId { get; set; }
        public IEnumerable<Company> Companies { get; set; }
    }
}