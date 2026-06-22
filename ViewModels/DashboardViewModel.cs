using System;
using System.Collections.Generic;

namespace PharmacyProMS.ViewModels
{
    public class DashboardViewModel
    {
        // ── Today Summary ──────────────────────────────────
        public decimal TodaySaleAmount { get; set; }
        public int TodaySaleCount { get; set; }
        public decimal TodayPurchaseAmount { get; set; }
        public decimal TodayProfit { get; set; }

        // ── Overall Summary ────────────────────────────────
        public int TotalMedicines { get; set; }
        public int TotalCustomers { get; set; }
        public int TotalSuppliers { get; set; }
        public int LowStockCount { get; set; }
        public int OutOfStockCount { get; set; }
        public int ExpiringCount { get; set; }
        public int ExpiredCount { get; set; }

        // ── Monthly Chart Data ─────────────────────────────
        public List<MonthlyData> MonthlySales { get; set; }
        public List<MonthlyData> MonthlyPurchases { get; set; }

        // ── Top Medicines ──────────────────────────────────
        public List<TopMedicineData> TopMedicines { get; set; }

        // ── Recent Invoices ────────────────────────────────
        public List<RecentInvoiceData> RecentSales { get; set; }

        // ── Expiring Soon ──────────────────────────────────
        public List<ExpiryAlertData> ExpiringBatches { get; set; }

        // ── Low Stock ─────────────────────────────────────
        public List<LowStockData> LowStockMedicines { get; set; }
    }

    public class MonthlyData
    {
        public string Month { get; set; }
        public decimal Amount { get; set; }
    }

    public class TopMedicineData
    {
        public string MedicineName { get; set; }
        public int TotalQty { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class RecentInvoiceData
    {
        public string InvoiceNumber { get; set; }
        public string CustomerName { get; set; }
        public decimal NetAmount { get; set; }
        public DateTime SaleDate { get; set; }
    }

    public class ExpiryAlertData
    {
        public string MedicineName { get; set; }
        public string BatchNumber { get; set; }
        public DateTime ExpiryDate { get; set; }
        public int Quantity { get; set; }
        public int DaysLeft { get; set; }
    }

    public class LowStockData
    {
        public string MedicineName { get; set; }
        public int CurrentStock { get; set; }
        public int ReOrderLevel { get; set; }
        public string CompanyName { get; set; }
    }
}