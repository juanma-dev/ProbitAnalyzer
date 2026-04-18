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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using ProbitAnalyzer.Models;
using ProbitAnalyzer.Services;
using ProbitAnalyzer.ViewModels;
using ProbitAnalyzer.Views;

namespace ProbitAnalyzer;

/// <summary>
/// Main window code-behind for the Probit Analyzer application.
/// </summary>
public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();
        _viewModel = new MainViewModel();
        DataContext = _viewModel;
    }

    private void Calculate_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.Calculate();

        if (_viewModel.HasResults)
        {
            // Update chart
            var validPoints = _viewModel.DataPoints
                .Where(p => p.Concentration > 0 && p.Mortality > 0 && p.Mortality < 100)
                .ToList();

            probitChart.DataPoints = validPoints;
            probitChart.Results = _viewModel.Results;
            probitChart.DrawChart();
        }
    }

    private void AddRow_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.AddRow();
    }

    private void RemoveRow_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is ProbitDataPoint point)
        {
            _viewModel.RemoveRow(point);
        }
    }

    private void ClearAll_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.ClearAll();
        probitChart.DataPoints = null;
        probitChart.Results = null;
        probitChart.DrawChart();
    }

    private void LoadExample_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.LoadExampleData();
        probitChart.DataPoints = null;
        probitChart.Results = null;
        probitChart.DrawChart();
    }

    // ═══════════ Menu Handlers ═══════════

    private void MenuButton_Click(object sender, RoutedEventArgs e)
    {
        menuPopup.IsOpen = !menuPopup.IsOpen;
    }

    private void Export_Click(object sender, RoutedEventArgs e)
    {
        menuPopup.IsOpen = false;

        if (!_viewModel.HasResults || _viewModel.Results == null)
        {
            MessageBox.Show(
                "No hay resultados para exportar.\nPrimero calcule el análisis probit.",
                "Exportar",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        var dialog = new SaveFileDialog
        {
            Title = "Exportar Análisis Probit",
            Filter = "Excel Workbook (*.xlsx)|*.xlsx",
            DefaultExt = ".xlsx",
            FileName = $"Probit_Analysis_{DateTime.Now:yyyyMMdd_HHmmss}",
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                var validPoints = _viewModel.DataPoints
                    .Where(p => p.Concentration > 0 && p.Mortality > 0 && p.Mortality < 100)
                    .ToList();

                // Capture chart as PNG image
                MemoryStream? chartStream = CaptureChartAsImage();

                ExcelExporter.Export(dialog.FileName, validPoints, _viewModel.Results, chartStream);

                chartStream?.Dispose();

                _viewModel.StatusMessage = $"✓ Exportado: {Path.GetFileName(dialog.FileName)}";

                MessageBox.Show(
                    $"Archivo exportado exitosamente:\n{dialog.FileName}",
                    "Exportar",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error al exportar: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }

    /// <summary>
    /// Captures the ProbitChart canvas as a PNG image in a MemoryStream.
    /// </summary>
    private MemoryStream? CaptureChartAsImage()
    {
        try
        {
            if (probitChart.ActualWidth < 10 || probitChart.ActualHeight < 10)
                return null;

            double dpi = 96 * 2; // 2x resolution for crisp export
            double width = probitChart.ActualWidth;
            double height = probitChart.ActualHeight;

            var renderBitmap = new RenderTargetBitmap(
                (int)(width * 2), (int)(height * 2),
                dpi, dpi,
                PixelFormats.Pbgra32);

            renderBitmap.Render(probitChart);

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(renderBitmap));

            var stream = new MemoryStream();
            encoder.Save(stream);
            stream.Position = 0;

            return stream;
        }
        catch
        {
            return null;
        }
    }

    private void About_Click(object sender, RoutedEventArgs e)
    {
        menuPopup.IsOpen = false;
        var aboutWindow = new AboutWindow { Owner = this };
        aboutWindow.ShowDialog();
    }
}