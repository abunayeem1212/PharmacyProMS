using OfficeOpenXml;
using OfficeOpenXml.Style;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace PharmacyProMS.Helpers
{
    public static class ExcelHelper
    {
        public static byte[] GenerateExcel(
            string sheetName,
            List<string> headers,
            List<List<string>> rows,
            string title = "")
        {
            using (var package = new ExcelPackage())
            {
                var ws = package.Workbook.Worksheets
                    .Add(sheetName);

                int startRow = 1;

                // Title Row
                if (!string.IsNullOrEmpty(title))
                {
                    ws.Cells[1, 1, 1, headers.Count]
                        .Merge = true;
                    ws.Cells[1, 1].Value = title;
                    ws.Cells[1, 1].Style.Font.Bold = true;
                    ws.Cells[1, 1].Style.Font.Size = 14;
                    ws.Cells[1, 1].Style.HorizontalAlignment
                        = ExcelHorizontalAlignment.Center;
                    ws.Cells[1, 1].Style.Fill.PatternType
                        = ExcelFillStyle.Solid;
                    ws.Cells[1, 1].Style.Fill
                        .BackgroundColor
                        .SetColor(Color.FromArgb(26, 138, 90));
                    ws.Cells[1, 1].Style.Font
                        .Color.SetColor(Color.White);
                    startRow = 2;
                }

                // Header Row
                for (int c = 0; c < headers.Count; c++)
                {
                    var cell = ws.Cells[startRow, c + 1];
                    cell.Value = headers[c];
                    cell.Style.Font.Bold = true;
                    cell.Style.Fill.PatternType
                        = ExcelFillStyle.Solid;
                    cell.Style.Fill.BackgroundColor
                        .SetColor(Color.FromArgb(232, 245, 238));
                    cell.Style.Font.Color
                        .SetColor(Color.FromArgb(13, 92, 58));
                    cell.Style.Border.Bottom.Style
                        = ExcelBorderStyle.Thin;
                }

                // Data Rows
                for (int r = 0; r < rows.Count; r++)
                {
                    for (int c = 0; c < rows[r].Count; c++)
                    {
                        ws.Cells[startRow + r + 1, c + 1]
                            .Value = rows[r][c];
                    }

                    // Alternate row color
                    if (r % 2 == 0)
                    {
                        ws.Cells[startRow + r + 1, 1,
                                 startRow + r + 1,
                                 headers.Count]
                            .Style.Fill.PatternType
                            = ExcelFillStyle.Solid;
                        ws.Cells[startRow + r + 1, 1,
                                 startRow + r + 1,
                                 headers.Count]
                            .Style.Fill.BackgroundColor
                            .SetColor(Color.FromArgb(
                                248, 249, 250));
                    }
                }

                // Auto fit columns
                ws.Cells[ws.Dimension.Address]
                    .AutoFitColumns();

                // Border
                ws.Cells[startRow, 1,
                         startRow + rows.Count,
                         headers.Count]
                    .Style.Border.BorderAround(
                        ExcelBorderStyle.Thin);

                return package.GetAsByteArray();
            }
        }
    }
}