using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace DeskLite.Services;

public static class PomodoroRingHelper
{
    public static void Update(Ellipse ring, double percent, double diameter, double strokeThickness)
    {
        var radius = (diameter - strokeThickness) / 2.0;
        var circumference = 2 * Math.PI * radius;
        var filled = circumference * Math.Clamp(percent, 0, 100) / 100.0;
        ring.StrokeDashArray = new DoubleCollection { filled, circumference };
        ring.StrokeDashOffset = 0;
    }
}
