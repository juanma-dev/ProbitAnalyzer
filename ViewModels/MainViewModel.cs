using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using ProbitAnalyzer.Models;
using ProbitAnalyzer.Services;

namespace ProbitAnalyzer.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private ProbitResults? _results;
    private bool _hasResults;
    private string _errorMessage = "";
    private bool _hasError;
    private bool _isCalculating;
    private string _statusMessage = "Ingrese sus datos y presione Calcular";

    public ObservableCollection<ProbitDataPoint> DataPoints { get; } = new();

    public ProbitResults? Results
    {
        get => _results;
        set { _results = value; OnPropertyChanged(nameof(Results)); }
    }

    public bool HasResults
    {
        get => _hasResults;
        set { _hasResults = value; OnPropertyChanged(nameof(HasResults)); }
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set { _errorMessage = value; OnPropertyChanged(nameof(ErrorMessage)); }
    }

    public bool HasError
    {
        get => _hasError;
        set { _hasError = value; OnPropertyChanged(nameof(HasError)); }
    }

    public bool IsCalculating
    {
        get => _isCalculating;
        set { _isCalculating = value; OnPropertyChanged(nameof(IsCalculating)); }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set { _statusMessage = value; OnPropertyChanged(nameof(StatusMessage)); }
    }

    public MainViewModel()
    {
        // Initialize with sample data for demonstration
        AddSampleData();
    }

    private void AddSampleData()
    {
        var sampleData = new (double conc, double mort)[]
        {
            (1.0, 5),
            (2.5, 15),
            (5.0, 35),
            (10.0, 55),
            (25.0, 78),
            (50.0, 92),
        };

        for (int i = 0; i < sampleData.Length; i++)
        {
            DataPoints.Add(new ProbitDataPoint
            {
                Index = i + 1,
                Concentration = sampleData[i].conc,
                Mortality = sampleData[i].mort
            });
        }
    }

    public void AddRow()
    {
        DataPoints.Add(new ProbitDataPoint { Index = DataPoints.Count + 1 });
        ReindexRows();
    }

    public void RemoveRow(ProbitDataPoint point)
    {
        DataPoints.Remove(point);
        ReindexRows();
    }

    public void ClearAll()
    {
        DataPoints.Clear();
        Results = null;
        HasResults = false;
        HasError = false;
        ErrorMessage = "";
        StatusMessage = "Datos limpiados. Ingrese nuevos datos.";
    }

    private void ReindexRows()
    {
        for (int i = 0; i < DataPoints.Count; i++)
            DataPoints[i].Index = i + 1;
    }

    public void Calculate()
    {
        HasError = false;
        ErrorMessage = "";
        IsCalculating = true;
        StatusMessage = "Calculando análisis probit...";

        try
        {
            var validPoints = DataPoints
                .Where(p => p.Concentration > 0 && p.Mortality > 0 && p.Mortality < 100)
                .ToList();

            if (validPoints.Count < 2)
            {
                HasError = true;
                ErrorMessage = "Se necesitan al menos 2 puntos válidos.\n" +
                              "• Concentración debe ser > 0\n" +
                              "• Mortalidad debe estar entre 0% y 100% (exclusivo)";
                HasResults = false;
                StatusMessage = "Error en los datos.";
                return;
            }

            Results = ProbitCalculator.Analyze(validPoints);
            HasResults = true;
            StatusMessage = $"✓ Análisis completado — LC₅₀ = {Results.LC50:F4} µL/L";
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Error en el cálculo: {ex.Message}";
            HasResults = false;
            StatusMessage = "Error en el cálculo.";
        }
        finally
        {
            IsCalculating = false;
        }
    }

    public void LoadExampleData()
    {
        DataPoints.Clear();
        HasResults = false;
        HasError = false;

        // Typical bioassay data (insecticide toxicity test)
        var exampleData = new (double conc, double mort)[]
        {
            (0.5, 3),
            (1.0, 10),
            (2.0, 22),
            (5.0, 45),
            (10.0, 68),
            (20.0, 85),
            (50.0, 95),
        };

        for (int i = 0; i < exampleData.Length; i++)
        {
            DataPoints.Add(new ProbitDataPoint
            {
                Index = i + 1,
                Concentration = exampleData[i].conc,
                Mortality = exampleData[i].mort
            });
        }

        StatusMessage = "Datos de ejemplo cargados. Presione Calcular.";
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged(string name) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
