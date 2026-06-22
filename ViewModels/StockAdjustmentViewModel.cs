using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using PharmacyProMS.Models;

namespace PharmacyProMS.ViewModels
{
    public class StockAdjustmentCreateViewModel
    {
        [Required(ErrorMessage = "Medicine is required")]
        [Display(Name = "Medicine")]
        public int MedicineId { get; set; }

        [Required(ErrorMessage = "Type is required")]
        [Display(Name = "Adjustment Type")]
        public string AdjustmentType { get; set; }

        [Required(ErrorMessage = "Quantity is required")]
        [Range(1, 99999,
            ErrorMessage = "Quantity must be > 0")]
        [Display(Name = "Quantity")]
        public int Quantity { get; set; }

        [Required(ErrorMessage = "Reason is required")]
        [Display(Name = "Reason")]
        public string Reason { get; set; }

        [Display(Name = "Note")]
        public string Note { get; set; }

        // For Dropdown
        public IEnumerable<Medicine> Medicines
        { get; set; }

        // Current Stock (AJAX এ আসবে)
        public int CurrentStock { get; set; }
    }

    public class StockAdjustmentListViewModel
    {
        public IEnumerable<StockAdjustment> Adjustments
        { get; set; }
        public string SearchTerm { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int TotalCount { get; set; }
        public int PageSize { get; set; } = 10;
        public string FilterType { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
    }
}