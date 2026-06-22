using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using PharmacyProMS.Models;

namespace PharmacyProMS.ViewModels
{
    public class SaleItemViewModel
    {
        public int MedicineId { get; set; }
        public string MedicineName { get; set; }
        public string GenericName { get; set; }
        public int Quantity { get; set; }
        public decimal SalePrice { get; set; }
        public decimal ItemDiscount { get; set; } = 0;
        public decimal SubTotal { get; set; }
        public int BatchId { get; set; }
        public string BatchNumber { get; set; }
        public string ExpiryDate { get; set; }
    }

    public class SaleCreateViewModel
    {
        public string InvoiceNumber { get; set; }
        public DateTime SaleDate { get; set; }
            = DateTime.Today;
        public int? CustomerId { get; set; }
        public string WalkInCustomerName { get; set; }
        public decimal Discount { get; set; } = 0;
        public decimal VatPercentage { get; set; } = 0;
        public bool IsCreditSale { get; set; } = false;
        public decimal PaidAmount { get; set; } = 0;
        public string ItemsJson { get; set; }
        public string PaymentMethod { get; set; } = "Cash";
        public string Note { get; set; }

        // Dropdowns
        public IEnumerable<Customer> Customers { get; set; }
        // Medicine dropdown নেই — Search দিয়ে হবে

        public IEnumerable<Medicine> Medicines { get; set; }

    }

    public class SaleListViewModel
    {
        public IEnumerable<SaleInvoice> Invoices { get; set; }
        public string SearchTerm { get; set; }
        public string SortBy { get; set; }
        public string SortOrder { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int TotalCount { get; set; }
        public int PageSize { get; set; } = 10;
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }

        // Summary
        public decimal TotalSaleAmount { get; set; }
        public decimal TotalVatAmount { get; set; }
        public decimal TotalProfit { get; set; }
    }


}