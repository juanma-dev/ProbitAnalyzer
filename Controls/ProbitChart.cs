using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using ProbitAnalyzer.Models;

namespace ProbitAnalyzer.Controls;

/// <summary>
/// Custom chart control for displaying the probit regression line and data points.
/// </summary>
public class ProbitChart : Canvas
{
    private const double Padding = 60;
    private const double RightPadding = 30;
    private const double TopPadding = 30;

    public static readonly DependencyProperty DataPointsProperty =
        DependencyProperty.Register("DataPoints", typeof(List<ProbitDataPoint>), typeof(ProbitChart),
            new PropertyMetadata(null, OnDataChanged));

    public static readonly DependencyProperty ResultsProperty =
        DependencyProperty.Register("Results", typeof(ProbitResults), typeof(ProbitChart),
            new PropertyMetadata(null, OnDataChanged));

    public List<ProbitDataPoint>? DataPoints
    {
        get => (List<ProbitDataPoint>?)GetValue(DataPointsProperty);
        set => SetValue(DataPointsProperty, value);
    }

    public ProbitResults? Results
    {
        get => (ProbitResults?)GetValue(ResultsProperty);
        set => SetValue(ResultsProperty, value);
    }

    private static void OnDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ProbitChart chart)
            chart.DrawChart();
    }

    public ProbitChart()
    {
        Background = new SolidColorBrush(Color.FromRgb(15, 17, 23));
        SizeChanged += (s, e) => DrawChart();
        ClipToBounds = true;
    }

    public void DrawChart()
    {
        Children.Clear();

        double w = ActualWidth;
        double h = ActualHeight;
        if (w < 100 || h < 100) return;

        var points = DataPoints?.Where(p => p.Concentration > 0 && p.Mortality > 0 && p.Mortality < 100).ToList();
        if (points == null || points.Count == 0)
        {
            DrawEmptyState(w, h);
            return;
        }

        double chartW = w - Padding - RightPadding;
        double chartH = h - Padding - TopPadding;

        // Determine axis ranges
        double minX = points.Min(p => p.LogConcentration);
        double maxX = points.Max(p => p.LogConcentration);
        double minY = points.Min(p => p.ProbitValue);
        double maxY = points.Max(p => p.ProbitValue);

        // Add padding to ranges
        double xRange = maxX - minX;
        double yRange = maxY - minY;
        if (xRange < 0.1) xRange = 1;
        if (yRange < 0.1) yRange = 1;
        minX -= xRange * 0.15;
        maxX += xRange * 0.15;
        minY -= yRange * 0.15;
        maxY += yRange * 0.15;

        // Draw grid and axes
        DrawGrid(w, h, chartW, chartH, minX, maxX, minY, maxY);

        // Draw regression line
        if (Results != null)
        {
            DrawRegressionLine(chartW, chartH, minX, maxX, minY, maxY);
            DrawLC50Line(chartW, chartH, minX, maxX, minY, maxY);
            DrawLC95Line(chartW, chartH, minX, maxX, minY, maxY);
        }

        // Draw data points
        foreach (var point in points)
        {
            double px = Padding + (point.LogConcentration - minX) / (maxX - minX) * chartW;
            double py = TopPadding + (1 - (point.ProbitValue - minY) / (maxY - minY)) * chartH;

            // Glow effect
            var glow = new Ellipse
            {
                Width = 20, Height = 20,
                Fill = new RadialGradientBrush(
                    Color.FromArgb(80, 99, 179, 237),
                    Color.FromArgb(0, 99, 179, 237)),
            };
            SetLeft(glow, px - 10);
            SetTop(glow, py - 10);
            Children.Add(glow);

            // Data point
            var ellipse = new Ellipse
            {
                Width = 10, Height = 10,
                Fill = new SolidColorBrush(Color.FromRgb(99, 179, 237)),
                Stroke = new SolidColorBrush(Color.FromRgb(255, 255, 255)),
                StrokeThickness = 1.5,
            };
            SetLeft(ellipse, px - 5);
            SetTop(ellipse, py - 5);
            Children.Add(ellipse);
        }

        // Axis labels
        DrawAxisLabels(w, h);
    }

    private void DrawEmptyState(double w, double h)
    {
        var text = new TextBlock
        {
            Text = "📊 Ingrese datos y calcule para ver el gráfico",
            Foreground = new SolidColorBrush(Color.FromRgb(120, 130, 150)),
            FontSize = 14,
            FontStyle = FontStyles.Italic,
        };
        text.Measure(new Size(w, h));
        SetLeft(text, (w - text.DesiredSize.Width) / 2);
        SetTop(text, (h - text.DesiredSize.Height) / 2);
        Children.Add(text);
    }

    private void DrawGrid(double w, double h, double chartW, double chartH,
                           double minX, double maxX, double minY, double maxY)
    {
        var gridBrush = new SolidColorBrush(Color.FromArgb(30, 255, 255, 255));
        var axisBrush = new SolidColorBrush(Color.FromArgb(80, 255, 255, 255));
        var textBrush = new SolidColorBrush(Color.FromRgb(160, 170, 185));

        // Horizontal grid lines
        int numYLines = 6;
        for (int i = 0; i <= numYLines; i++)
        {
            double yVal = minY + (maxY - minY) * i / numYLines;
            double py = TopPadding + (1 - (double)i / numYLines) * chartH;

            var line = new Line
            {
                X1 = Padding, Y1 = py,
                X2 = Padding + chartW, Y2 = py,
                Stroke = i == 0 ? axisBrush : gridBrush,
                StrokeThickness = i == 0 ? 1.5 : 0.5,
            };
            Children.Add(line);

            var label = new TextBlock
            {
                Text = yVal.ToString("F2"),
                Foreground = textBrush,
                FontSize = 10,
                TextAlignment = TextAlignment.Right,
                Width = 45,
            };
            SetLeft(label, Padding - 50);
            SetTop(label, py - 7);
            Children.Add(label);
        }

        // Vertical grid lines
        int numXLines = 6;
        for (int i = 0; i <= numXLines; i++)
        {
            double xVal = minX + (maxX - minX) * i / numXLines;
            double px = Padding + (double)i / numXLines * chartW;

            var line = new Line
            {
                X1 = px, Y1 = TopPadding,
                X2 = px, Y2 = TopPadding + chartH,
                Stroke = i == 0 ? axisBrush : gridBrush,
                StrokeThickness = i == 0 ? 1.5 : 0.5,
            };
            Children.Add(line);

            var label = new TextBlock
            {
                Text = xVal.ToString("F2"),
                Foreground = textBrush,
                FontSize = 10,
                TextAlignment = TextAlignment.Center,
                Width = 50,
            };
            SetLeft(label, px - 25);
            SetTop(label, TopPadding + chartH + 5);
            Children.Add(label);
        }
    }

    private void DrawAxisLabels(double w, double h)
    {
        var labelBrush = new SolidColorBrush(Color.FromRgb(200, 210, 225));

        // X-axis label
        var xLabel = new TextBlock
        {
            Text = "Log₁₀(Concentración)",
            Foreground = labelBrush,
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
        };
        xLabel.Measure(new Size(w, h));
        SetLeft(xLabel, Padding + (w - Padding - RightPadding - xLabel.DesiredSize.Width) / 2);
        SetTop(xLabel, h - 20);
        Children.Add(xLabel);

        // Y-axis label (rotated)
        var yLabel = new TextBlock
        {
            Text = "Probit (Mortalidad)",
            Foreground = labelBrush,
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
            RenderTransform = new RotateTransform(-90),
            RenderTransformOrigin = new Point(0.5, 0.5),
        };
        yLabel.Measure(new Size(h, w));
        SetLeft(yLabel, -12);
        SetTop(yLabel, TopPadding + (h - TopPadding - Padding) / 2 + yLabel.DesiredSize.Width / 2);
        Children.Add(yLabel);
    }

    private void DrawRegressionLine(double chartW, double chartH,
                                    double minX, double maxX, double minY, double maxY)
    {
        if (Results == null) return;

        double x1 = minX;
        double x2 = maxX;
        double y1 = Results.Intercept + Results.Slope * x1;
        double y2 = Results.Intercept + Results.Slope * x2;

        // Clamp to visible area
        double px1 = Padding + (x1 - minX) / (maxX - minX) * chartW;
        double py1 = TopPadding + (1 - (y1 - minY) / (maxY - minY)) * chartH;
        double px2 = Padding + (x2 - minX) / (maxX - minX) * chartW;
        double py2 = TopPadding + (1 - (y2 - minY) / (maxY - minY)) * chartH;

        // Gradient line
        var line = new Line
        {
            X1 = px1, Y1 = py1,
            X2 = px2, Y2 = py2,
            Stroke = new LinearGradientBrush(
                Color.FromRgb(236, 72, 153),
                Color.FromRgb(168, 85, 247),
                new Point(0, 0), new Point(1, 1)),
            StrokeThickness = 2.5,
            Opacity = 0.9,
        };
        Children.Add(line);
    }

    private void DrawLC50Line(double chartW, double chartH,
                               double minX, double maxX, double minY, double maxY)
    {
        if (Results == null) return;
        DrawLCLine(Results.LC50, 5.0, chartW, chartH, minX, maxX, minY, maxY,
                   Color.FromRgb(52, 211, 153), "LC₅₀");
    }

    private void DrawLC95Line(double chartW, double chartH,
                               double minX, double maxX, double minY, double maxY)
    {
        if (Results == null) return;
        double probit95 = ProbitDataPoint.ProbitTransform(0.95);
        DrawLCLine(Results.LC95, probit95, chartW, chartH, minX, maxX, minY, maxY,
                   Color.FromRgb(251, 146, 60), "LC₉₅");
    }

    private void DrawLCLine(double lcValue, double probitValue,
                            double chartW, double chartH,
                            double minX, double maxX, double minY, double maxY,
                            Color color, string label)
    {
        double logLC = Math.Log10(lcValue);
        if (logLC < minX || logLC > maxX) return;
        if (probitValue < minY || probitValue > maxY) return;

        double px = Padding + (logLC - minX) / (maxX - minX) * chartW;
        double py = TopPadding + (1 - (probitValue - minY) / (maxY - minY)) * chartH;
        var brush = new SolidColorBrush(color);
        var dashStyle = new DoubleCollection { 4, 3 };

        // Vertical line
        var vLine = new Line
        {
            X1 = px, Y1 = py,
            X2 = px, Y2 = TopPadding + chartH,
            Stroke = brush,
            StrokeThickness = 1.5,
            StrokeDashArray = dashStyle,
            Opacity = 0.7,
        };
        Children.Add(vLine);

        // Horizontal line
        var hLine = new Line
        {
            X1 = Padding, Y1 = py,
            X2 = px, Y2 = py,
            Stroke = brush,
            StrokeThickness = 1.5,
            StrokeDashArray = dashStyle,
            Opacity = 0.7,
        };
        Children.Add(hLine);

        // Intersection dot
        var dot = new Ellipse
        {
            Width = 12, Height = 12,
            Fill = brush,
            Stroke = Brushes.White,
            StrokeThickness = 2,
        };
        SetLeft(dot, px - 6);
        SetTop(dot, py - 6);
        Children.Add(dot);

        // Label
        var textBlock = new TextBlock
        {
            Text = $"{label} = {lcValue:F2}",
            Foreground = brush,
            FontSize = 11,
            FontWeight = FontWeights.Bold,
        };
        textBlock.Measure(new Size(300, 30));
        double labelX = px + 8;
        double labelY = py - 20;
        if (labelX + textBlock.DesiredSize.Width > Padding + chartW)
            labelX = px - textBlock.DesiredSize.Width - 8;
        SetLeft(textBlock, labelX);
        SetTop(textBlock, labelY);
        Children.Add(textBlock);
    }
}
