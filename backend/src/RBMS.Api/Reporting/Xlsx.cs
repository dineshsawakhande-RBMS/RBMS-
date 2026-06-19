using ClosedXML.Excel;

namespace RBMS.Api.Reporting;

/// <summary>Builds a single-sheet .xlsx from headers + string rows (report exports).</summary>
public static class Xlsx
{
    public const string ContentType =
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    public static byte[] Build(string sheetName, IReadOnlyList<string> headers, IEnumerable<IReadOnlyList<string>> rows)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add(sheetName.Length > 31 ? sheetName[..31] : sheetName);

        for (var c = 0; c < headers.Count; c++)
        {
            var cell = ws.Cell(1, c + 1);
            cell.Value = headers[c];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#EEF0FB");
        }

        var r = 2;
        foreach (var row in rows)
        {
            for (var c = 0; c < row.Count; c++)
            {
                var text = row[c];
                var cell = ws.Cell(r, c + 1);
                if (decimal.TryParse(text, out var num))
                    cell.Value = num;
                else
                    cell.Value = text;
            }
            r++;
        }

        ws.Columns().AdjustToContents();
        ws.SheetView.FreezeRows(1);

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }
}
