using System.Windows;
using System.Windows.Media;

namespace DeskLite.Services;

public static class PomodoroRingHelper
{
    public static void Update(System.Windows.Shapes.Path ring, double percent, double diameter, double strokeThickness)
    {
        UpdateOpenArc(ring, percent, diameter, strokeThickness);
    }

    public static void UpdateOpenArc(
        System.Windows.Shapes.Path ring,
        double percent,
        double diameter,
        double strokeThickness,
        double arcDegrees = 285,
        double startDegrees = 128)
    {
        var clampedPercent = Math.Clamp(percent, 0, 100) / 100.0;
        var sweep = Math.Clamp(arcDegrees, 0.1, 359.9) * clampedPercent;
        ring.Data = CreateArcGeometry(diameter, strokeThickness, startDegrees, sweep);
    }

    private static Geometry CreateArcGeometry(double diameter, double strokeThickness, double startDegrees, double sweepDegrees)
    {
        if (sweepDegrees <= 0.1)
        {
            return Geometry.Empty;
        }

        var radius = (diameter - strokeThickness) / 2.0;
        var center = new System.Windows.Point(diameter / 2.0, diameter / 2.0);
        var start = PointOnCircle(center, radius, startDegrees);
        var end = PointOnCircle(center, radius, startDegrees + sweepDegrees);
        var figure = new PathFigure
        {
            StartPoint = start,
            IsClosed = false,
            IsFilled = false
        };
        figure.Segments.Add(new ArcSegment
        {
            Point = end,
            Size = new System.Windows.Size(radius, radius),
            SweepDirection = SweepDirection.Clockwise,
            IsLargeArc = sweepDegrees > 180
        });

        return new PathGeometry([figure]);
    }

    private static System.Windows.Point PointOnCircle(System.Windows.Point center, double radius, double degrees)
    {
        var radians = degrees * Math.PI / 180.0;
        return new System.Windows.Point(
            center.X + radius * Math.Cos(radians),
            center.Y + radius * Math.Sin(radians));
    }
}
