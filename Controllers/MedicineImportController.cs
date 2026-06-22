using Newtonsoft.Json;
using OfficeOpenXml;
using PharmacyProMS.Data;
using PharmacyProMS.Models;
using PharmacyProMS.ViewModels;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace PharmacyProMS.Controllers
{
    [Authorize(Roles = "Admin,Pharmacist")]
    public class MedicineImportController : Controller
    {
        private ApplicationDbContext db
            = new ApplicationDbContext();

        // ─── INDEX ────────────────────────────────────────────
        public ActionResult Index()
        {
            return View();
        }

        // ─── DOWNLOAD TEMPLATE ────────────────────────────────
        public ActionResult DownloadTemplate()
        {
            using (var package = new ExcelPackage())
            {
                var ws = package.Workbook.Worksheets
                    .Add("Medicines");

                // ── Header Row ─────────────────────────────
                string[] headers = {
                    "MedicineName",
                    "GenericName",
                    "CompanyName",
                    "CategoryName",
                    "UnitType",
                    "SalePrice",
                    "ReOrderLevel",
                    "Barcode"
                };

                for (int c = 0; c < headers.Length; c++)
                {
                    var cell = ws.Cells[1, c + 1];
                    cell.Value = headers[c];

                    // Header Style
                    cell.Style.Font.Bold = true;
                    cell.Style.Font.Color
                        .SetColor(Color.White);
                    cell.Style.Fill.PatternType
                        = OfficeOpenXml.Style
                            .ExcelFillStyle.Solid;
                    cell.Style.Fill.BackgroundColor
                        .SetColor(Color.FromArgb(
                            26, 138, 90));
                    cell.Style.Border.Bottom.Style
                        = OfficeOpenXml.Style
                            .ExcelBorderStyle.Thin;
                }

                // ── Sample Data (3 rows) ───────────────────
                var sampleData = new List<string[]>
                {
                    new[] {
                        "Napa 500mg", "Paracetamol",
                        "Beximco", "Tablet",
                        "Tablet", "5.00", "10", "8901234567890"
                    },
                    new[] {
                        "Seclo 20mg", "Omeprazole",
                        "Square", "Capsule",
                        "Capsule", "8.00", "10", ""
                    },
                    new[] {
                        "Amodis 400mg", "Metronidazole",
                        "ACI", "Tablet",
                        "Tablet", "6.00", "10", ""
                    }
                };

                for (int r = 0; r < sampleData.Count; r++)
                {
                    for (int c = 0;
                         c < sampleData[r].Length; c++)
                    {
                        ws.Cells[r + 2, c + 1].Value
                            = sampleData[r][c];
                    }

                    // Alternate row color
                    if (r % 2 == 0)
                    {
                        ws.Cells[r + 2, 1, r + 2,
                            headers.Length]
                            .Style.Fill.PatternType
                            = OfficeOpenXml.Style
                                .ExcelFillStyle.Solid;
                        ws.Cells[r + 2, 1, r + 2,
                            headers.Length]
                            .Style.Fill.BackgroundColor
                            .SetColor(Color.FromArgb(
                                248, 249, 250));
                    }
                }

                // ── Instructions Sheet ─────────────────────
                var wsInfo = package.Workbook.Worksheets
                    .Add("Instructions");

                wsInfo.Cells[1, 1].Value
                    = "📋 INSTRUCTIONS";
                wsInfo.Cells[1, 1].Style.Font.Bold = true;
                wsInfo.Cells[1, 1].Style.Font.Size = 14;

                var instructions = new[]
                {
                    "",
                    "1. Fill in the 'Medicines' sheet",
                    "2. MedicineName → Required",
                    "3. CompanyName → Required",
                    "4. CategoryName → Required " +
                       "(Tablet/Syrup/Capsule/Injection" +
                       "/Cream/Drop/Inhaler/Powder/Other)",
                    "5. UnitType → Required " +
                       "(Tablet/Syrup/Capsule/Injection" +
                       "/Cream/Drop/Inhaler/Powder/Other)",
                    "6. SalePrice → Required (numbers only)",
                    "7. GenericName → Optional",
                    "8. ReOrderLevel → Optional (default: 10)",
                    "9. Barcode → Optional",
                    "",
                    "⚠️ Do NOT change the header row!",
                    "⚠️ Do NOT add extra columns!",
                    "✅ You can add as many rows as needed"
                };

                for (int r = 0; r < instructions.Length; r++)
                    wsInfo.Cells[r + 1, 1].Value
                        = instructions[r];

                wsInfo.Column(1).Width = 70;

                // ── Auto fit ──────────────────────────────
                ws.Cells[ws.Dimension.Address]
                    .AutoFitColumns();

                var bytes = package.GetAsByteArray();

                return File(bytes,
                    "application/vnd.openxmlformats" +
                    "-officedocument.spreadsheetml.sheet",
                    "MedicineImportTemplate.xlsx");
            }
        }

        // ─── UPLOAD & PREVIEW ─────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Preview(
            HttpPostedFileBase excelFile)
        {
            if (excelFile == null ||
                excelFile.ContentLength == 0)
            {
                TempData["Error"] =
                    "Please select an Excel file!";
                return RedirectToAction("Index");
            }

            // Extension check
            string ext = Path.GetExtension(
                excelFile.FileName).ToLower();
            if (ext != ".xlsx" && ext != ".xls")
            {
                TempData["Error"] =
                    "Only Excel files (.xlsx, .xls) allowed!";
                return RedirectToAction("Index");
            }

            var validRows = new List<MedicineImportRow>();
            var invalidRows = new List<MedicineImportRow>();

            try
            {
                using (var package = new ExcelPackage(
                    excelFile.InputStream))
                {
                    var ws = package.Workbook.Worksheets
                        .FirstOrDefault();

                    if (ws == null)
                    {
                        TempData["Error"] =
                            "Excel file is empty!";
                        return RedirectToAction("Index");
                    }

                    int rowCount = ws.Dimension?.Rows ?? 0;

                    if (rowCount < 2)
                    {
                        TempData["Error"] =
                            "No data found in Excel!";
                        return RedirectToAction("Index");
                    }

                    // Row 2 থেকে শুরু (Row 1 = Header)
                    for (int row = 2; row <= rowCount; row++)
                    {
                        string medName = GetCellValue(
                            ws, row, 1);

                        // Empty row skip করো
                        if (string.IsNullOrWhiteSpace(medName))
                            continue;

                        var importRow = new MedicineImportRow
                        {
                            RowNumber = row,
                            MedicineName = medName.Trim(),
                            GenericName = GetCellValue(
                                ws, row, 2),
                            CompanyName = GetCellValue(
                                ws, row, 3),
                            CategoryName = GetCellValue(
                                ws, row, 4),
                            UnitType = GetCellValue(
                                ws, row, 5),
                            Barcode = GetCellValue(
                                ws, row, 8)
                        };

                        // Price parse করো
                        string priceStr = GetCellValue(
                            ws, row, 6);
                        if (decimal.TryParse(priceStr,
                            out decimal price) && price > 0)
                            importRow.SalePrice = price;
                        else
                        {
                            importRow.SalePrice = 0;
                        }

                        // ReOrderLevel parse করো
                        string reorderStr = GetCellValue(
                            ws, row, 7);
                        if (int.TryParse(reorderStr,
                            out int reorder))
                            importRow.ReOrderLevel = reorder;
                        else
                            importRow.ReOrderLevel = 10;

                        // Validation করো
                        ValidateRow(importRow);

                        if (importRow.IsValid)
                            validRows.Add(importRow);
                        else
                            invalidRows.Add(importRow);
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] =
                    "Error reading Excel: " + ex.Message;
                return RedirectToAction("Index");
            }

            if (!validRows.Any() && !invalidRows.Any())
            {
                TempData["Error"] =
                    "No data found in Excel file!";
                return RedirectToAction("Index");
            }

            var model = new MedicineImportPreviewViewModel
            {
                ValidRows = validRows,
                InvalidRows = invalidRows,
                TotalRows = validRows.Count
                                 + invalidRows.Count,
                ValidCount = validRows.Count,
                InvalidCount = invalidRows.Count,
                ImportDataJson = JsonConvert
                    .SerializeObject(validRows)
            };

            return View("Preview", model);
        }

        // ─── CONFIRM IMPORT ───────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ConfirmImport(string importDataJson)
        {
            if (string.IsNullOrEmpty(importDataJson))
            {
                TempData["Error"] = "No data to import!";
                return RedirectToAction("Index");
            }

            var rows = JsonConvert
                .DeserializeObject<List<MedicineImportRow>>(
                    importDataJson);

            if (rows == null || !rows.Any())
            {
                TempData["Error"] = "No data to import!";
                return RedirectToAction("Index");
            }

            int successCount = 0;
            int skipCount = 0;
            var errors = new List<string>();

            foreach (var row in rows)
            {
                try
                {
                    // Company find or create
                    var company = db.Companies
                        .FirstOrDefault(c =>
                            c.CompanyName.Trim().ToLower()
                            == row.CompanyName.Trim().ToLower());

                    if (company == null)
                    {
                        company = new Company
                        {
                            CompanyName = row.CompanyName.Trim(),
                            IsActive = true,
                            CreatedAt = DateTime.Now
                        };
                        db.Companies.Add(company);
                        db.SaveChanges();
                    }

                    // Category find or create
                    var category = db.MedicineCategories
                        .FirstOrDefault(c =>
                            c.CategoryName.Trim().ToLower()
                            == row.CategoryName.Trim().ToLower());

                    if (category == null)
                    {
                        category = new MedicineCategory
                        {
                            CategoryName = row.CategoryName.Trim(),
                            IsActive = true
                        };
                        db.MedicineCategories.Add(category);
                        db.SaveChanges();
                    }

                    // Duplicate medicine check
                    bool exists = db.Medicines.Any(m =>
                        m.MedicineName.Trim().ToLower()
                        == row.MedicineName.Trim().ToLower() &&
                        m.CompanyId == company.CompanyId);

                    if (exists)
                    {
                        skipCount++;
                        continue;
                    }

                    // Medicine add করো
                    var medicine = new Medicine
                    {
                        MedicineName = row.MedicineName.Trim(),
                        GenericName = string.IsNullOrEmpty(
                            row.GenericName)
                            ? null
                            : row.GenericName.Trim(),
                        UnitType = string.IsNullOrEmpty(
                            row.UnitType)
                            ? "Tablet"
                            : row.UnitType.Trim(),
                        SalePrice = row.SalePrice,
                        ReOrderLevel = row.ReOrderLevel > 0
                            ? row.ReOrderLevel : 10,
                        CurrentStock = 0,
                        Barcode = string.IsNullOrEmpty(
                            row.Barcode)
                            ? null
                            : row.Barcode.Trim(),
                        IsPrescriptionRequired = false,
                        IsActive = true,
                        CompanyId = company.CompanyId,
                        CategoryId = category.CategoryId,
                        CreatedAt = DateTime.Now
                    };

                    db.Medicines.Add(medicine);
                    successCount++;
                }
                catch (Exception ex)
                {
                    errors.Add("Row " + row.RowNumber
                        + ": " + ex.Message);
                }
            }

            db.SaveChanges();

            // Activity Log
            db.UserActivityLogs.Add(new UserActivityLog
            {
                UserId = User.Identity.Name,
                UserName = User.Identity.Name,
                Action = "Import",
                Description = successCount
                    + " medicines imported from Excel",
                TableAffected = "Medicines",
                LoggedAt = DateTime.Now
            });
            db.SaveChanges();

            TempData["ImportSuccess"] = successCount;
            TempData["ImportSkip"] = skipCount;
            TempData["ImportErrors"] = errors.Count;

            return RedirectToAction("Result");
        }

        // ─── RESULT ───────────────────────────────────────────
        public ActionResult Result()
        {
            ViewBag.SuccessCount = TempData["ImportSuccess"]
                ?? 0;
            ViewBag.SkipCount = TempData["ImportSkip"]
                ?? 0;
            ViewBag.ErrorCount = TempData["ImportErrors"]
                ?? 0;
            return View();
        }

        // ─── HELPER: Cell Value ───────────────────────────────
        private string GetCellValue(
            OfficeOpenXml.ExcelWorksheet ws,
            int row, int col)
        {
            var cell = ws.Cells[row, col];
            return cell.Value?.ToString()?.Trim() ?? "";
        }

        // ─── HELPER: Validate Row ─────────────────────────────
        private void ValidateRow(MedicineImportRow row)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(row.MedicineName))
                errors.Add("Medicine Name is required");

            if (string.IsNullOrWhiteSpace(row.CompanyName))
                errors.Add("Company Name is required");

            if (string.IsNullOrWhiteSpace(row.CategoryName))
                errors.Add("Category is required");

            if (string.IsNullOrWhiteSpace(row.UnitType))
                errors.Add("Unit Type is required");

            if (row.SalePrice <= 0)
                errors.Add("Valid Sale Price is required");

            if (errors.Any())
            {
                row.IsValid = false;
                row.ErrorMessage = string.Join(", ", errors);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}