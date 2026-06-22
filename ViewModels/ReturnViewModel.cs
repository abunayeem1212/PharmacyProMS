using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using PharmacyProMS.Models;

namespace PharmacyProMS.ViewModels
{
    // ── Sale Return Item ───────────────────────────────────
    public class SaleReturnItemViewModel
    {
        public int MedicineId { get; set; }
        public string MedicineName { get; set; }
        public int MaxQty { get; set; }
        public int Quantity { get; set; }
        public decimal SalePrice { get; set; }
        public decimal RefundAmount { get; set; }
    }

    // ── Sale Return Create ─────────────────────────────────
    public class SaleReturnCreateViewModel
    {
        public string ReturnNumber { get; set; }

        [Required(ErrorMessage = "Return date is required")]
        [DataType(DataType.Date)]
        [Display(Name = "Return Date")]
        public DateTime ReturnDate { get; set; }
            = DateTime.Today;

        [Display(Name = "Reason")]
        public string Reason { get; set; }

        [Required(ErrorMessage = "Invoice is required")]
        [Display(Name = "Sale Invoice")]
        public int InvoiceId { get; set; }

        public string ItemsJson { get; set; }

        // Invoice Info (display only)
        public string InvoiceNumber { get; set; }
        public DateTime SaleDate { get; set; }
        public string CustomerName { get; set; }
        public decimal InvoiceTotal { get; set; }

        // Invoice Items to return
        public List<SaleReturnItemViewModel> InvoiceItems
        { get; set; }
    }

    // ── Sale Return List ───────────────────────────────────
    public class SaleReturnListViewModel
    {
        public IEnumerable<SaleReturn> Returns { get; set; }
        public string SearchTerm { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int TotalCount { get; set; }
        public int PageSize { get; set; }
            = 10;
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public decimal TotalRefund { get; set; }
    }

    // ── Purchase Return Item ───────────────────────────────
    public class PurchaseReturnItemViewModel
    {
        public int MedicineId { get; set; }
        public string MedicineName { get; set; }
        public int MaxQty { get; set; }
        public int Quantity { get; set; }
        public decimal PurchasePrice { get; set; }
        public decimal ReturnAmount { get; set; }
    }

    // ── Purchase Return Create ─────────────────────────────
    public class PurchaseReturnCreateViewModel
    {
        public string ReturnNumber { get; set; }

        [Required(ErrorMessage = "Return date is required")]
        [DataType(DataType.Date)]
        [Display(Name = "Return Date")]
        public DateTime ReturnDate { get; set; }
            = DateTime.Today;

        [Display(Name = "Reason")]
        public string Reason { get; set; }

        [Required(ErrorMessage = "Invoice is required")]
        [Display(Name = "Purchase Invoice")]
        public int PurchaseId { get; set; }

        public string ItemsJson { get; set; }

        // Invoice Info
        public string InvoiceNumber { get; set; }
        public DateTime PurchaseDate { get; set; }
        public string SupplierName { get; set; }
        public decimal InvoiceTotal { get; set; }

        // Items
        public List<PurchaseReturnItemViewModel> InvoiceItems
        { get; set; }
    }

    // ── Purchase Return List ───────────────────────────────
    public class PurchaseReturnListViewModel
    {
        public IEnumerable<PurchaseReturn> Returns { get; set; }
        public string SearchTerm { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int TotalCount { get; set; }
        public int PageSize { get; set; }
            = 10;
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public decimal TotalReturn { get; set; }
    }
}