using System.Collections.Generic;

namespace PharmacyProMS.ViewModels
{
    // একটা Row এর Data
    public class MedicineImportRow
    {
        public int RowNumber { get; set; }
        public string MedicineName { get; set; }
        public string GenericName { get; set; }
        public string CompanyName { get; set; }
        public string CategoryName { get; set; }
        public string UnitType { get; set; }
        public decimal SalePrice { get; set; }
        public int ReOrderLevel { get; set; }
        public string Barcode { get; set; }

        // Validation
        public bool IsValid { get; set; } = true;
        public string ErrorMessage { get; set; }
    }

    // Preview Page এর Model
    public class MedicineImportPreviewViewModel
    {
        public List<MedicineImportRow> ValidRows
        { get; set; }
        public List<MedicineImportRow> InvalidRows
        { get; set; }
        public int TotalRows { get; set; }
        public int ValidCount { get; set; }
        public int InvalidCount { get; set; }

        // Confirm এর জন্য JSON
        public string ImportDataJson { get; set; }
    }
}