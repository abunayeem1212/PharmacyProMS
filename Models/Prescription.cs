using System;
using System.ComponentModel.DataAnnotations;

namespace PharmacyProMS.Models
{
    public class Prescription
    {
        public int PrescriptionId { get; set; }

        [Required]
        [Display(Name = "Doctor Name")]
        [StringLength(100)]
        public string DoctorName { get; set; }

        [Display(Name = "Prescription Date")]
        [DataType(DataType.Date)]
        public DateTime PrescriptionDate { get; set; } = DateTime.Today;

        [Display(Name = "Prescription Image")]
        public string ImagePath { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Foreign Key
        public int CustomerId { get; set; }

        // Navigation
        public virtual Customer Customer { get; set; }
    }
}