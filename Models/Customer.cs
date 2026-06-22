using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PharmacyProMS.Models
{
    public class Customer
    {
        public int CustomerId { get; set; }

        [Required(ErrorMessage = "Customer Name is required")]
        [StringLength(100)]
        [Display(Name = "Customer Name")]
        public string CustomerName { get; set; }

        [Phone]
        public string Phone { get; set; }

        [StringLength(200)]
        public string Address { get; set; }

        [EmailAddress]
        public string Email { get; set; }

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation
        public virtual ICollection<SaleInvoice> SaleInvoices { get; set; }
        public virtual ICollection<Prescription> Prescriptions { get; set; }
    }
}