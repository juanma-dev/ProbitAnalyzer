using System.ComponentModel;

namespace ProbitAnalyzer.Models;

/// <summary>
/// Represents a single data point for probit analysis.
/// </summary>
public class ProbitDataPoint : INotifyPropertyChanged
{
    private double _concentration;
    private double _mortality;
    private double _logConcentration;
    private double _probitValue;
    private int _index;

    public int Index
    {
        get => _index;
        set { _index = value; OnPropertyChanged(nameof(Index)); }
    }

    public double Concentration
    {
        get => _concentration;
        set
        {
            _concentration = value;
            OnPropertyChanged(nameof(Concentration));
            if (value > 0)
            {
                LogConcentration = Math.Log10(value);
            }
        }
    }

    public double Mortality
    {
        get => _mortality;
        set
        {
            _mortality = value;
            OnPropertyChanged(nameof(Mortality));
            if (value > 0 && value < 100)
            {
                ProbitValue = ProbitTransform(value / 100.0);
            }
        }
    }

    public double LogConcentration
    {
        get => _logConcentration;
        set { _logConcentration = value; OnPropertyChanged(nameof(LogConcentration)); }
    }

    public double ProbitValue
    {
        get => _probitValue;
        set { _probitValue = value; OnPropertyChanged(nameof(ProbitValue)); }
    }

    /// <summary>
    /// Probit transformation: Probit(p) = Φ⁻¹(p) + 5
    /// Uses the rational approximation for the inverse normal CDF.
    /// </summary>
    public static double ProbitTransform(double p)
    {
        return InverseNormalCDF(p) + 5.0;
    }

    /// <summary>
    /// Inverse of the standard normal CDF using Abramowitz and Stegun approximation.
    /// </summary>
    private static double InverseNormalCDF(double p)
    {
        if (p <= 0) return double.NegativeInfinity;
        if (p >= 1) return double.PositiveInfinity;

        // Rational approximation for lower region
        const double a1 = -3.969683028665376e+01;
        const double a2 = 2.209460984245205e+02;
        const double a3 = -2.759285104469687e+02;
        const double a4 = 1.383577518672690e+02;
        const double a5 = -3.066479806614716e+01;
        const double a6 = 2.506628277459239e+00;

        const double b1 = -5.447609879822406e+01;
        const double b2 = 1.615858368580409e+02;
        const double b3 = -1.556989798598866e+02;
        const double b4 = 6.680131188771972e+01;
        const double b5 = -1.328068155288572e+01;

        const double c1 = -7.784894002430293e-03;
        const double c2 = -3.223964580411365e-01;
        const double c3 = -2.400758277161838e+00;
        const double c4 = -2.549732539343734e+00;
        const double c5 = 4.374664141464968e+00;
        const double c6 = 2.938163982698783e+00;

        const double d1 = 7.784695709041462e-03;
        const double d2 = 3.224671290700398e-01;
        const double d3 = 2.445134137142996e+00;
        const double d4 = 3.754408661907416e+00;

        const double pLow = 0.02425;
        const double pHigh = 1 - pLow;

        double q, r;

        if (p < pLow)
        {
            q = Math.Sqrt(-2 * Math.Log(p));
            return (((((c1 * q + c2) * q + c3) * q + c4) * q + c5) * q + c6) /
                   ((((d1 * q + d2) * q + d3) * q + d4) * q + 1);
        }
        else if (p <= pHigh)
        {
            q = p - 0.5;
            r = q * q;
            return (((((a1 * r + a2) * r + a3) * r + a4) * r + a5) * r + a6) * q /
                   (((((b1 * r + b2) * r + b3) * r + b4) * r + b5) * r + 1);
        }
        else
        {
            q = Math.Sqrt(-2 * Math.Log(1 - p));
            return -(((((c1 * q + c2) * q + c3) * q + c4) * q + c5) * q + c6) /
                    ((((d1 * q + d2) * q + d3) * q + d4) * q + 1);
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged(string name) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

/// <summary>
/// Results of the probit regression analysis.
/// </summary>
public class ProbitResults
{
    public double Intercept { get; set; }       // a (intercepto)
    public double Slope { get; set; }            // b (pendiente)
    public double RSquared { get; set; }         // R²
    public double LC50 { get; set; }             // Concentración Letal 50%
    public double LC95 { get; set; }             // Concentración Letal 95%
    public double LC10 { get; set; }             // Concentración Letal 10%
    public double LC90 { get; set; }             // Concentración Letal 90%
    public double StandardErrorSlope { get; set; }
    public double StandardErrorIntercept { get; set; }
    public double Chi2 { get; set; }             // Chi-cuadrado
    public string Equation => $"Y = {Intercept:F4} + {Slope:F4}·X";

    /// <summary>
    /// Calculate lethal concentration for any mortality percentage.
    /// </summary>
    public double GetLC(double mortalityPercent)
    {
        double probit = ProbitDataPoint.ProbitTransform(mortalityPercent / 100.0);
        double logLC = (probit - Intercept) / Slope;
        return Math.Pow(10, logLC);
    }
}
