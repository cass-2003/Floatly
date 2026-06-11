using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace DeskLite.Services;

public static class PomodoroRingHelper
{
    public static void Update(Ellipse ring, double percent, double diameter, double strokeThickness)
    {
        UpdateOpenArc(ring, percent, diameter, strokeThickness);
    }

    public static void UpdateOpenArc(
        Ellipse ring,
        double percent,
        double diameter,
        double strokeThickness,
        double arcPercent = 82)
    {
        var radius = (diameter - strokeThickness) / 2.0;
        var circumference = 2 * Math.PI * radius;
        var visibleArc = circumference * Math.Clamp(arcPercent, 0, 100) / 100.0;
        var gap = Math.Max(0, circumference - visibleArc);
        var filled = visibleArc * Math.Clamp(percent, 0, 100) / 100.0;
        ring.StrokeDashArray = new DoubleCollection { filled, Math.Max(0, visibleArc - filled) + gap };
        ring.StrokeDashOffset = 0;
    }
}
