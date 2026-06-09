using ClosedXML.Excel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Globalization;

public static class ExcelTranslationReader
{
    public static List<Language_Replace.TranslationEntry> ReadTranslationsFromExcel(string path)
    {
        var result = new List<Language_Replace.TranslationEntry>();

        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return result;

        using var wb = new XLWorkbook(path);

        IXLWorksheet ws = null;
        foreach (var w in wb.Worksheets)
        {
            if (w != null && !w.IsEmpty())
            {
                ws = w;
                break;
            }
        }
        if (ws == null)
            return result;

        var lastUsed = ws.LastRowUsed();
        if (lastUsed == null)
            return result;

        int lastRow = lastUsed.RowNumber();

        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        for (int row = 1; row <= lastRow; row++)
        {
            string? colOriginal = SafeCell(ws, row, 2);
            string? colTranslation = SafeCell(ws, row, 3);

            if (string.IsNullOrWhiteSpace(colOriginal) && string.IsNullOrWhiteSpace(colTranslation))
                continue;

            if (string.IsNullOrWhiteSpace(colOriginal))
                continue; 

            var original = colOriginal.Trim();
            var translation = (colTranslation ?? "").Trim();

            map[original] = translation;
        }

        foreach (var kv in map)
        {
            result.Add(new Language_Replace.TranslationEntry
            {
                Original = kv.Key,
                Translation = kv.Value
            });
        }

        return result;
    }

    private static string? SafeCell(IXLWorksheet ws, int row, int col)
    {
        var cell = ws.Cell(row, col);

        if (cell.IsEmpty())
            return null;

        switch (cell.DataType)
        {
            case XLDataType.DateTime:
                return cell.GetDateTime().ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

            case XLDataType.Number:
                return cell.GetDouble().ToString(CultureInfo.InvariantCulture);

            case XLDataType.Boolean:
                return cell.GetBoolean() ? "true" : "false";

            case XLDataType.TimeSpan:
                return cell.GetTimeSpan().ToString();

            case XLDataType.Text:
            default:
                var txt = cell.GetString();
                return string.IsNullOrWhiteSpace(txt) ? null : txt;
        }
    }

}