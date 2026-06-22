using System.Collections.Generic;
using PharmacyProMS.Models;

namespace PharmacyProMS.ViewModels
{
    public class SupplierDueSummary
    {
        public int SupplierId { get; set; }
        public string SupplierName { get; set; }
        public string Phone { get; set; }
        public string CompanyName { get; set; }
        public decimal TotalPurchase { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal TotalDue { get; set; }
    }

    public class SupplierLedgerIndexViewModel
    {
        public List<SupplierDueSummary> Suppliers { get; set; }
        public decimal GrandDue { get; set; }
        public int DueCount { get; set; }
        public int TotalCount { get; set; }
    }
}