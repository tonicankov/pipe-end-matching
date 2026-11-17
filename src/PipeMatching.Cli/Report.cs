using System.Globalization;
using System.Text;
using PipeMatching.Core;

namespace PipeMatching.Cli;

/// <summary>
/// File output. Everything is written with the invariant culture, so decimals use a dot
/// even on machines set to a locale that uses a comma (e.g. Dutch or Bulgarian).
/// </summary>
internal static class Report
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    public static string JointsCsv(IReadOnlyList<PlacedPipe> sequence, IReadOnlyList<Pipe> pipes, CostMatrix costs)
    {
        var csv = new StringBuilder();
        csv.AppendLine("joint,from_pipe,from_flipped,to_pipe,to_flipped,rotation_deg,hilo_mm");
        for (var i = 1; i < sequence.Count; i++)
        {
            var from = sequence[i - 1];
            var to = sequence[i];
            csv.AppendLine(string.Create(Inv,
                $"{i},{pipes[from.PipeIndex].Id},{from.Flipped},{pipes[to.PipeIndex].Id},{to.Flipped},{costs.RotationDegrees(from, to):F1},{costs.HiLoMm(from, to):F4}"));
        }

        return csv.ToString();
    }

    /// <summary>
    /// Polar plot of the two ends at a joint after the best rotation. Deviations from the mean
    /// radius are exaggerated so millimetres are visible on a 570 mm pipe.
    /// </summary>
    public static string JointSvg(PlacedPipe from, PlacedPipe to, IReadOnlyList<Pipe> pipes, CostMatrix costs)
    {
        const double size = 520;
        const double center = size / 2;
        const double baseRadiusPx = 170;
        const double pxPerMm = 35;

        var fixedEnd = from.ExitEnd(pipes[from.PipeIndex]);
        var rotating = to.EntryEnd(pipes[to.PipeIndex]);
        var shift = costs.ShiftSteps(from, to);
        var n = fixedEnd.Count;
        var mean = (fixedEnd.MeanRadius + rotating.MeanRadius) / 2;
        var steps = HiLo.AroundJoint(fixedEnd, rotating, shift);
        var worstAt = Array.IndexOf(steps, steps.Max());

        (double X, double Y) Point(int index, double radiusMm)
        {
            var angle = 2 * Math.PI * index / n;
            var r = baseRadiusPx + (radiusMm - mean) * pxPerMm;
            return (center + r * Math.Cos(angle), center - r * Math.Sin(angle));
        }

        string Polyline(Func<int, double> radiusAt)
        {
            var points = Enumerable.Range(0, n + 1).Select(i => Point(i % n, radiusAt(i % n)));
            return string.Join(" ", points.Select(p => string.Create(Inv, $"{p.X:F1},{p.Y:F1}")));
        }

        var a = Point(worstAt, fixedEnd[worstAt]);
        var b = Point(worstAt, rotating[(worstAt + shift) % n]);

        var svg = new StringBuilder();
        svg.AppendLine(string.Create(Inv, $"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{size}\" height=\"{size + 60}\" font-family=\"sans-serif\" font-size=\"13\">"));
        svg.AppendLine("<rect width=\"100%\" height=\"100%\" fill=\"white\"/>");
        svg.AppendLine(string.Create(Inv, $"<circle cx=\"{center}\" cy=\"{center}\" r=\"{baseRadiusPx}\" fill=\"none\" stroke=\"#ccc\" stroke-dasharray=\"4 4\"/>"));
        svg.AppendLine($"<polyline fill=\"none\" stroke=\"#1f6feb\" stroke-width=\"2\" points=\"{Polyline(i => fixedEnd[i])}\"/>");
        svg.AppendLine($"<polyline fill=\"none\" stroke=\"#e8590c\" stroke-width=\"2\" points=\"{Polyline(i => rotating[(i + shift) % n])}\"/>");
        svg.AppendLine(string.Create(Inv, $"<line x1=\"{a.X:F1}\" y1=\"{a.Y:F1}\" x2=\"{b.X:F1}\" y2=\"{b.Y:F1}\" stroke=\"#d00\" stroke-width=\"4\"/>"));
        svg.AppendLine(string.Create(Inv,
            $"<text x=\"16\" y=\"{size + 10}\">Worst joint: pipe {pipes[from.PipeIndex].Id} (blue) to pipe {pipes[to.PipeIndex].Id} (orange), turned {costs.RotationDegrees(from, to):F1} deg</text>"));
        svg.AppendLine(string.Create(Inv,
            $"<text x=\"16\" y=\"{size + 30}\">Max hi-lo {steps[worstAt]:F3} mm (red) at {worstAt * 360.0 / n:F0} deg. Deviation from mean radius exaggerated {pxPerMm:F0} px/mm.</text>"));
        svg.AppendLine("</svg>");
        return svg.ToString();
    }
}
