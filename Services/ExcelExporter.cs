// Probit Analyzer — Análisis de Concentración Letal
// Copyright (C) 2026 Juanma
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.

using System.IO;
using ClosedXML.Excel;
using ProbitAnalyzer.Models;

namespace ProbitAnalyzer.Services;

/// <summary>
/// Service to export probit analysis results to Excel (.xlsx) format.
/// Includes all data, all calculated results, and the regression chart.
/// </summary>
public static class ExcelExporter
{
    // Color constants matching the app theme
    private static readonly XLColor HeaderBg = XLColor.FromHtml("#6366F1");
    private static readonly XLColor SubHeaderBg = XLColor.FromHtml("#1E2235");
    private static readonly XLColor GreenAccent = XLColor.FromHtml("#34D399");
    private static readonly XLColor OrangeAccent = XLColor.FromHtml("#FB923C");
    private static readonly XLColor BlueAccent = XLColor.FromHtml("#63B3ED");
    private static readonly XLColor SkyAccent = XLColor.FromHtml("#38BDF8");
    private static readonly XLColor PurpleAccent = XLColor.FromHtml("#A855F7");
    private static readonly XLColor RowEven = XLColor.FromHtml("#F8FAFC");
    private static readonly XLColor RowOdd = XLColor.White;

    /// <summary>
    /// Exports the data points, all results, and chart image to an Excel file.
    /// </summary>
    public static void Export(string filePath, List<ProbitDataPoint> dataPoints,
        ProbitResults results, MemoryStream? chartImage = null)
    {
        using var workbook = new XLWorkbook();

        CreateDataSheet(workbook, dataPoints, results);
        CreateResultsSheet(workbook, dataPoints, results, chartImage);

        workbook.SaveAs(filePath);
    }

    /// <summary>
    /// Sheet 1: All raw and transformed data.
    /// </summary>
    private static void CreateDataSheet(XLWorkbook workbook, List<ProbitDataPoint> dataPoints,
        ProbitResults results)
    {
        var ws = workbook.Worksheets.Add("Datos");

        // ── Title ──
        ws.Cell("A1").Value = "ANÁLISIS PROBIT — DATOS DE ENTRADA Y TRANSFORMACIONES";
        ws.Range("A1:G1").Merge();
        StyleTitle(ws.Cell("A1"));

        // ── Subtitle ──
        ws.Cell("A2").Value = $"Fecha: {DateTime.Now:dd/MM/yyyy HH:mm:ss}   |   Ecuación: {results.Equation}";
        ws.Range("A2:G2").Merge();
        ws.Cell("A2").Style.Font.Italic = true;
        ws.Cell("A2").Style.Font.FontColor = XLColor.DarkGray;
        ws.Cell("A2").Style.Font.FontSize = 10;

        // ── Headers ──
        var headers = new[] {
            "#", "Concentración (µL/L)", "Mortalidad (%)",
            "Log₁₀(Concentración)", "Probit (Observado)",
            "Probit (Predicho)", "Residual"
        };
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(4, i + 1);
            cell.Value = headers[i];
            StyleColumnHeader(cell);
        }

        // ── Data Rows ──
        var validPoints = dataPoints
            .Where(p => p.Concentration > 0 && p.Mortality > 0 && p.Mortality < 100)
            .ToList();

        for (int i = 0; i < validPoints.Count; i++)
        {
            int row = i + 5;
            var point = validPoints[i];
            double predicted = results.Intercept + results.Slope * point.LogConcentration;
            double residual = point.ProbitValue - predicted;

            ws.Cell(row, 1).Value = i + 1;
            ws.Cell(row, 2).Value = point.Concentration;
            ws.Cell(row, 3).Value = point.Mortality;
            ws.Cell(row, 4).Value = point.LogConcentration;
            ws.Cell(row, 5).Value = point.ProbitValue;
            ws.Cell(row, 6).Value = predicted;
            ws.Cell(row, 7).Value = residual;

            var bgColor = i % 2 == 0 ? RowEven : RowOdd;
            ws.Range(row, 1, row, 7).Style.Fill.BackgroundColor = bgColor;
            ws.Range(row, 1, row, 7).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        // Number formats
        ws.Column(2).Style.NumberFormat.Format = "0.0000";
        ws.Column(3).Style.NumberFormat.Format = "0.00";
        ws.Column(4).Style.NumberFormat.Format = "0.0000";
        ws.Column(5).Style.NumberFormat.Format = "0.0000";
        ws.Column(6).Style.NumberFormat.Format = "0.0000";
        ws.Column(7).Style.NumberFormat.Format = "0.0000";

        // Table border
        int lastDataRow = 4 + validPoints.Count;
        var dataRange = ws.Range(4, 1, lastDataRow, 7);
        dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        dataRange.Style.Border.OutsideBorderColor = XLColor.LightGray;
        dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        dataRange.Style.Border.InsideBorderColor = XLColor.FromHtml("#E2E8F0");

        ws.Columns().AdjustToContents();
    }

    /// <summary>
    /// Sheet 2: All results, statistics, and chart image.
    /// </summary>
    private static void CreateResultsSheet(XLWorkbook workbook, List<ProbitDataPoint> dataPoints,
        ProbitResults results, MemoryStream? chartImage)
    {
        var ws = workbook.Worksheets.Add("Resultados");

        // ── Title ──
        ws.Cell("A1").Value = "ANÁLISIS PROBIT — RESULTADOS COMPLETOS";
        ws.Range("A1:D1").Merge();
        StyleTitle(ws.Cell("A1"));

        int currentRow = 3;

        // ═══════════════════════════════════════════════════════
        // SECTION 1: Ecuación de Regresión
        // ═══════════════════════════════════════════════════════
        currentRow = AddSectionTitle(ws, currentRow, "📐 ECUACIÓN DE REGRESIÓN");
        ws.Cell(currentRow, 1).Value = "Modelo:";
        ws.Cell(currentRow, 1).Style.Font.Bold = true;
        ws.Cell(currentRow, 2).Value = "Y = a + b·X";
        ws.Cell(currentRow, 2).Style.Font.Italic = true;
        currentRow++;
        ws.Cell(currentRow, 1).Value = "Donde:";
        ws.Cell(currentRow, 1).Style.Font.Bold = true;
        ws.Cell(currentRow, 2).Value = "Y = Probit(Mortalidad), X = Log₁₀(Concentración)";
        currentRow++;
        ws.Cell(currentRow, 1).Value = "Resultado:";
        ws.Cell(currentRow, 1).Style.Font.Bold = true;
        ws.Cell(currentRow, 2).Value = results.Equation;
        ws.Cell(currentRow, 2).Style.Font.Bold = true;
        ws.Cell(currentRow, 2).Style.Font.FontColor = XLColor.FromHtml("#6366F1");
        ws.Cell(currentRow, 2).Style.Font.FontSize = 13;
        currentRow += 2;

        // ═══════════════════════════════════════════════════════
        // SECTION 2: Concentraciones Letales (MAIN RESULTS)
        // ═══════════════════════════════════════════════════════
        currentRow = AddSectionTitle(ws, currentRow, "☠️ CONCENTRACIONES LETALES (µL/L)");
        StyleSubHeader(ws, currentRow, "Parámetro", "Mortalidad (%)", "Valor (µL/L)", "Log₁₀(LC)");
        currentRow++;

        // LC10
        AddLCRow(ws, currentRow, "LC₁₀", 10, results.LC10, XLColor.DarkGray);
        currentRow++;

        // LC50 - highlighted
        AddLCRow(ws, currentRow, "LC₅₀", 50, results.LC50, GreenAccent);
        ws.Range(currentRow, 1, currentRow, 4).Style.Fill.BackgroundColor = XLColor.FromHtml("#ECFDF5");
        ws.Cell(currentRow, 3).Style.Font.FontSize = 14;
        currentRow++;

        // LC90
        AddLCRow(ws, currentRow, "LC₉₀", 90, results.LC90, XLColor.DarkGray);
        currentRow++;

        // LC95 - highlighted
        AddLCRow(ws, currentRow, "LC₉₅", 95, results.LC95, OrangeAccent);
        ws.Range(currentRow, 1, currentRow, 4).Style.Fill.BackgroundColor = XLColor.FromHtml("#FFF7ED");
        ws.Cell(currentRow, 3).Style.Font.FontSize = 14;
        currentRow += 2;

        // ═══════════════════════════════════════════════════════
        // SECTION 3: Parámetros de Regresión
        // ═══════════════════════════════════════════════════════
        currentRow = AddSectionTitle(ws, currentRow, "📊 PARÁMETROS DE REGRESIÓN");
        StyleSubHeader(ws, currentRow, "Parámetro", "Valor");
        currentRow++;

        currentRow = AddParamRow(ws, currentRow, "Intercepto (a)", results.Intercept.ToString("F6"), XLColor.Black, 0);
        currentRow = AddParamRow(ws, currentRow, "Pendiente (b)", results.Slope.ToString("F6"), XLColor.Black, 1);
        currentRow++;

        // ═══════════════════════════════════════════════════════
        // SECTION 4: Estadísticas de Ajuste
        // ═══════════════════════════════════════════════════════
        currentRow = AddSectionTitle(ws, currentRow, "📈 ESTADÍSTICAS DE AJUSTE");
        StyleSubHeader(ws, currentRow, "Parámetro", "Valor", "Interpretación");
        currentRow++;

        // R (Pearson)
        ws.Cell(currentRow, 1).Value = "R (Correlación de Pearson)";
        ws.Cell(currentRow, 2).Value = results.R;
        ws.Cell(currentRow, 2).Style.NumberFormat.Format = "0.000000";
        ws.Cell(currentRow, 2).Style.Font.Bold = true;
        ws.Cell(currentRow, 2).Style.Font.FontColor = SkyAccent;
        ws.Cell(currentRow, 3).Value = GetRInterpretation(results.R);
        ws.Range(currentRow, 1, currentRow, 3).Style.Fill.BackgroundColor = RowEven;
        currentRow++;

        // R²
        ws.Cell(currentRow, 1).Value = "R² (Coef. Determinación)";
        ws.Cell(currentRow, 2).Value = results.RSquared;
        ws.Cell(currentRow, 2).Style.NumberFormat.Format = "0.000000";
        ws.Cell(currentRow, 2).Style.Font.Bold = true;
        ws.Cell(currentRow, 2).Style.Font.FontColor = PurpleAccent;
        ws.Cell(currentRow, 3).Value = $"El modelo explica el {results.RSquared * 100:F2}% de la variabilidad";
        ws.Range(currentRow, 1, currentRow, 3).Style.Fill.BackgroundColor = RowOdd;
        currentRow++;

        // Standard Error Intercept
        ws.Cell(currentRow, 1).Value = "Error Estándar Intercepto";
        ws.Cell(currentRow, 2).Value = results.StandardErrorIntercept;
        ws.Cell(currentRow, 2).Style.NumberFormat.Format = "0.000000";
        ws.Cell(currentRow, 2).Style.Font.Bold = true;
        ws.Cell(currentRow, 3).Value = "Precisión del intercepto";
        ws.Range(currentRow, 1, currentRow, 3).Style.Fill.BackgroundColor = RowEven;
        currentRow++;

        // Standard Error Slope
        ws.Cell(currentRow, 1).Value = "Error Estándar Pendiente";
        ws.Cell(currentRow, 2).Value = results.StandardErrorSlope;
        ws.Cell(currentRow, 2).Style.NumberFormat.Format = "0.000000";
        ws.Cell(currentRow, 2).Style.Font.Bold = true;
        ws.Cell(currentRow, 3).Value = "Precisión de la pendiente";
        ws.Range(currentRow, 1, currentRow, 3).Style.Fill.BackgroundColor = RowOdd;
        currentRow++;

        // Chi-squared
        ws.Cell(currentRow, 1).Value = "χ² (Bondad de Ajuste)";
        ws.Cell(currentRow, 2).Value = results.Chi2;
        ws.Cell(currentRow, 2).Style.NumberFormat.Format = "0.0000";
        ws.Cell(currentRow, 2).Style.Font.Bold = true;
        ws.Cell(currentRow, 3).Value = "Menor valor = mejor ajuste";
        ws.Range(currentRow, 1, currentRow, 3).Style.Fill.BackgroundColor = RowEven;
        currentRow += 2;

        // ═══════════════════════════════════════════════════════
        // SECTION 5: Chart Image
        // ═══════════════════════════════════════════════════════
        if (chartImage != null && chartImage.Length > 0)
        {
            currentRow = AddSectionTitle(ws, currentRow, "📉 GRÁFICO DE REGRESIÓN PROBIT");
            currentRow++;

            chartImage.Position = 0;
            var picture = ws.AddPicture(chartImage, "ProbitChart")
                .MoveTo(ws.Cell(currentRow, 1))
                .WithSize(800, 450);
            currentRow += 24; // Skip rows for the image
        }


        // ── Footer ──
        ws.Cell(currentRow, 1).Value = $"Generado por Probit Analyzer v1.0.0 — {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
        ws.Cell(currentRow, 1).Style.Font.Italic = true;
        ws.Cell(currentRow, 1).Style.Font.FontColor = XLColor.Gray;
        ws.Cell(currentRow, 1).Style.Font.FontSize = 9;
        currentRow++;
        ws.Cell(currentRow, 1).Value = "Copyright © 2026 Juanma — Licencia GNU GPL v3";
        ws.Cell(currentRow, 1).Style.Font.Italic = true;
        ws.Cell(currentRow, 1).Style.Font.FontColor = XLColor.Gray;
        ws.Cell(currentRow, 1).Style.Font.FontSize = 9;
        currentRow++;
        ws.Cell(currentRow, 1).Value = "https://github.com/juanma-dev/ProbitAnalyzer";
        ws.Cell(currentRow, 1).Style.Font.FontColor = BlueAccent;
        ws.Cell(currentRow, 1).Style.Font.FontSize = 9;

        ws.Columns().AdjustToContents();
        // Ensure minimum column widths for readability
        if (ws.Column(1).Width < 30) ws.Column(1).Width = 30;
        if (ws.Column(2).Width < 18) ws.Column(2).Width = 18;
        if (ws.Column(3).Width < 25) ws.Column(3).Width = 25;
    }

    // ═══════════════════════════════════════════════════════
    // Helper methods
    // ═══════════════════════════════════════════════════════

    private static void StyleTitle(IXLCell cell)
    {
        cell.Style.Font.Bold = true;
        cell.Style.Font.FontSize = 14;
        cell.Style.Font.FontColor = XLColor.White;
        cell.Style.Fill.BackgroundColor = HeaderBg;
        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
    }

    private static void StyleColumnHeader(IXLCell cell)
    {
        cell.Style.Font.Bold = true;
        cell.Style.Font.FontColor = XLColor.White;
        cell.Style.Fill.BackgroundColor = SubHeaderBg;
        cell.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
        cell.Style.Border.BottomBorderColor = BlueAccent;
        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
    }

    private static int AddSectionTitle(IXLWorksheet ws, int row, string title)
    {
        ws.Cell(row, 1).Value = title;
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 1).Style.Font.FontSize = 12;
        ws.Cell(row, 1).Style.Font.FontColor = XLColor.FromHtml("#1E293B");
        ws.Range(row, 1, row, 4).Merge();
        ws.Range(row, 1, row, 4).Style.Border.BottomBorder = XLBorderStyleValues.Medium;
        ws.Range(row, 1, row, 4).Style.Border.BottomBorderColor = HeaderBg;
        return row + 1;
    }

    private static void StyleSubHeader(IXLWorksheet ws, int row, params string[] headers)
    {
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(row, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Fill.BackgroundColor = SubHeaderBg;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }
    }

    private static void AddLCRow(IXLWorksheet ws, int row, string name, double mortality,
        double lcValue, XLColor color)
    {
        ws.Cell(row, 1).Value = name;
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 1).Style.Font.FontColor = color;
        ws.Cell(row, 1).Style.Font.FontSize = 12;
        ws.Cell(row, 2).Value = mortality;
        ws.Cell(row, 2).Style.NumberFormat.Format = "0";
        ws.Cell(row, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Cell(row, 3).Value = lcValue;
        ws.Cell(row, 3).Style.NumberFormat.Format = "0.0000";
        ws.Cell(row, 3).Style.Font.Bold = true;
        ws.Cell(row, 3).Style.Font.FontColor = color;
        ws.Cell(row, 3).Style.Font.FontSize = 12;
        ws.Cell(row, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Cell(row, 4).Value = Math.Log10(lcValue);
        ws.Cell(row, 4).Style.NumberFormat.Format = "0.0000";
        ws.Cell(row, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
    }

    private static int AddParamRow(IXLWorksheet ws, int row, string name, string value,
        XLColor color, int index)
    {
        ws.Cell(row, 1).Value = name;
        ws.Cell(row, 2).Value = value;
        ws.Cell(row, 2).Style.Font.Bold = true;
        ws.Cell(row, 2).Style.Font.FontColor = color;
        ws.Range(row, 1, row, 2).Style.Fill.BackgroundColor = index % 2 == 0 ? RowEven : RowOdd;
        return row + 1;
    }

    private static string GetRInterpretation(double r)
    {
        double absR = Math.Abs(r);
        if (absR >= 0.99) return "Correlación casi perfecta";
        if (absR >= 0.95) return "Correlación muy fuerte";
        if (absR >= 0.80) return "Correlación fuerte";
        if (absR >= 0.60) return "Correlación moderada";
        if (absR >= 0.40) return "Correlación débil";
        return "Correlación muy débil";
    }
}
