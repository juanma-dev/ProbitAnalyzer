using System.Windows;
using System.Windows.Controls;
using ProbitAnalyzer.Models;
using ProbitAnalyzer.ViewModels;

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
}