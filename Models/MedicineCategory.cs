using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PharmacyProMS.Models
{
    public class MedicineCategory
    {
        [Key]
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "Category Name is required")]
        [StringLength(100)]
        [Display(Name = "Category Name")]
        public string CategoryName { get; set; }

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;

        // Navigation
        public virtual ICollection<Medicine> Medicines { get; set; }
    }
}