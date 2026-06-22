using System.Collections.Generic;
using PharmacyProMS.Models;

namespace PharmacyProMS.ViewModels
{
    public class CustomerDueSummary
    {
        public int CustomerId { get; set; }
        public string CustomerName { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        public decimal TotalSale { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal TotalDue { get; set; }
    }

    public class CustomerLedgerIndexViewModel
    {
        public List<CustomerDueSummary> Customers { get; set; }
        public decimal GrandTotal { get; set; }
        public int DueCount { get; set; }
        public int TotalCount { get; set; }
    }
}