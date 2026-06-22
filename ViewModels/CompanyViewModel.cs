using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web;
using PharmacyProMS.Models;

namespace PharmacyProMS.ViewModels
{
    public class CompanyViewModel
    {
        public int CompanyId { get; set; }

        [Required(ErrorMessage = "Company Name is required")]
        [StringLength(100, ErrorMessage = "Max 100 characters")]
        [Display(Name = "Company Name")]
        public string CompanyName { get; set; }

        [StringLength(200)]
        [Display(Name = "Address")]
        public string Address { get; set; }

        [Phone(ErrorMessage = "Invalid phone number")]
        [Display(Name = "Phone")]
        public string Phone { get; set; }

        [EmailAddress(ErrorMessage = "Invalid email address")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Company Logo")]
        public string LogoPath { get; set; }

        // Image Upload
        [Display(Name = "Upload Logo")]
        public HttpPostedFileBase LogoFile { get; set; }

        public DateTime CreatedAt { get; set; }
    }

    // Pagination + Search + Sort
    public class CompanyListViewModel
    {
        public IEnumerable<Company> Companies { get; set; }
        public string SearchTerm { get; set; }
        public string SortBy { get; set; }
        public string SortOrder { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int PageSize { get; set; } = 10;
        public int TotalCount { get; set; }
    }
}