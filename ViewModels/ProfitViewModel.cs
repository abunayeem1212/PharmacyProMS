namespace PharmacyProMS.ViewModels
{
    public class ProfitReportViewModel
    {
        public string MedicineName { get; set; }
        public int TotalQty { get; set; }
        public decimal TotalSale { get; set; }
        public decimal TotalCost { get; set; }
        public decimal TotalProfit { get; set; }
    }
}