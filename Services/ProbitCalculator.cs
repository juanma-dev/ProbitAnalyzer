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

using ProbitAnalyzer.Models;

namespace ProbitAnalyzer.Services;

/// <summary>
/// Performs Probit regression analysis using least squares method.
/// Y = a + bX where Y = Probit(mortality), X = log10(concentration)
/// </summary>
public static class ProbitCalculator
{
    /// <summary>
    /// Performs probit analysis on the given data points.
    /// </summary>
    public static ProbitResults Analyze(List<ProbitDataPoint> dataPoints)
    {
        if (dataPoints.Count < 2)
            throw new InvalidOperationException("Se necesitan al menos 2 puntos de datos para el análisis.");

        // Filter valid data points
        var validPoints = dataPoints
            .Where(p => p.Concentration > 0 && p.Mortality > 0 && p.Mortality < 100)
            .ToList();

        if (validPoints.Count < 2)
            throw new InvalidOperationException("Se necesitan al menos 2 puntos válidos (concentración > 0, 0 < mortalidad < 100).");

        int n = validPoints.Count;
        double[] x = validPoints.Select(p => p.LogConcentration).ToArray();
        double[] y = validPoints.Select(p => p.ProbitValue).ToArray();

        // Calculate means
        double meanX = x.Average();
        double meanY = y.Average();

        // Calculate regression coefficients using least squares
        double sumXY = 0, sumX2 = 0, sumY2 = 0;
        for (int i = 0; i < n; i++)
        {
            double dx = x[i] - meanX;
            double dy = y[i] - meanY;
            sumXY += dx * dy;
            sumX2 += dx * dx;
            sumY2 += dy * dy;
        }

        double slope = sumXY / sumX2;           // b
        double intercept = meanY - slope * meanX; // a

        // R-squared
        double ssReg = slope * slope * sumX2;
        double ssTot = sumY2;
        double rSquared = ssTot > 0 ? ssReg / ssTot : 0;

        // Standard errors
        double[] residuals = new double[n];
        double ssRes = 0;
        for (int i = 0; i < n; i++)
        {
            double predicted = intercept + slope * x[i];
            residuals[i] = y[i] - predicted;
            ssRes += residuals[i] * residuals[i];
        }

        double mse = n > 2 ? ssRes / (n - 2) : 0;
        double seSlope = sumX2 > 0 ? Math.Sqrt(mse / sumX2) : 0;
        double seIntercept = Math.Sqrt(mse * (1.0 / n + meanX * meanX / sumX2));

        // Chi-squared goodness of fit
        double chi2 = 0;
        for (int i = 0; i < n; i++)
        {
            double predicted = intercept + slope * x[i];
            if (predicted != 0)
                chi2 += (y[i] - predicted) * (y[i] - predicted) / Math.Abs(predicted);
        }

        // R = Pearson correlation coefficient (with sign of slope)
        double r = Math.Sign(slope) * Math.Sqrt(rSquared);

        var results = new ProbitResults
        {
            Intercept = intercept,
            Slope = slope,
            RSquared = rSquared,
            R = r,
            StandardErrorSlope = seSlope,
            StandardErrorIntercept = seIntercept,
            Chi2 = chi2
        };

        // Calculate lethal concentrations
        // LC50: Probit = 5.0 → 5 = a + b·log10(LC50)
        results.LC50 = results.GetLC(50);
        results.LC95 = results.GetLC(95);
        results.LC10 = results.GetLC(10);
        results.LC90 = results.GetLC(90);

        return results;
    }

    /// <summary>
    /// Generate regression line points for charting.
    /// </summary>
    public static (double[] xValues, double[] yValues) GetRegressionLine(
        ProbitResults results, double minX, double maxX, int points = 100)
    {
        double step = (maxX - minX) / (points - 1);
        double[] xVals = new double[points];
        double[] yVals = new double[points];

        for (int i = 0; i < points; i++)
        {
            xVals[i] = minX + i * step;
            yVals[i] = results.Intercept + results.Slope * xVals[i];
        }

        return (xVals, yVals);
    }
}
