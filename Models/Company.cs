using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PharmacyProMS.Models
{
    public class Company
    {
        public int CompanyId { get; set; }

        [Required(ErrorMessage = "Company Name is required")]
        [StringLength(100)]
        [Display(Name = "Company Name")]
        public string CompanyName { get; set; }

        [Display(Name = "Logo")]
        public string LogoPath { get; set; }

        [StringLength(200)]
        public string Address { get; set; }

        [Phone]
        [Display(Name = "Phone")]
        public string Phone { get; set; }

        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation Property
        public virtual ICollection<Medicine> Medicines { get; set; }
        public virtual ICollection<Supplier> Suppliers { get; set; }
    }
}