using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using PharmacyProMS.Models;

namespace PharmacyProMS.ViewModels
{
    // ── Single Item (Details Row) ──────────────────────────
    public class PurchaseItemViewModel
    {
        public int MedicineId { get; set; }
        public string MedicineName { get; set; }
        public string BatchNumber { get; set; }
        public string ExpiryDate { get; set; }
        public string ManufactureDate { get; set; }
        public int Quantity { get; set; }
        public decimal PurchasePrice { get; set; }
        public decimal SubTotal { get; set; }
    }

    // ── Master Form ────────────────────────────────────────
    public class PurchaseCreateViewModel
    {
        [Required(ErrorMessage = "Invoice number is required")]
        [Display(Name = "Invoice Number")]
        public string InvoiceNumber { get; set; }

        [Required(ErrorMessage = "Purchase date is required")]
        [DataType(DataType.Date)]
        [Display(Name = "Purchase Date")]
        public DateTime PurchaseDate { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "Supplier is required")]
        [Display(Name = "Supplier")]
        public int SupplierId { get; set; }

        [Display(Name = "Discount (৳)")]
        public decimal Discount { get; set; } = 0;

        [Display(Name = "Paid Amount (৳)")]
        public decimal PaidAmount { get; set; } = 0;

        // Items as JSON string (hidden field থেকে আসবে)
        public string ItemsJson { get; set; }

        // Dropdowns
        public IEnumerable<Supplier> Suppliers { get; set; }
        public IEnumerable<Medicine> Medicines { get; set; }
    }

    // ── List ───────────────────────────────────────────────
    public class PurchaseListViewModel
    {
        public IEnumerable<PurchaseInvoice> Invoices { get; set; }
        public string SearchTerm { get; set; }
        public string SortBy { get; set; }
        public string SortOrder { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int TotalCount { get; set; }
        public int PageSize { get; set; } = 10;
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public int? FilterSupplier { get; set; }
        public IEnumerable<Supplier> Suppliers { get; set; }

        // Summary
        public decimal TotalPurchaseAmount { get; set; }
        public decimal TotalPaidAmount { get; set; }
        public decimal TotalDueAmount { get; set; }
    }
}