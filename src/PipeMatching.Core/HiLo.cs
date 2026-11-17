namespace PipeMatching.Core;

/// <summary>Best way to turn one pipe end against another.</summary>
/// <param name="ShiftSteps">How many samples the rotating end is turned.</param>
/// <param name="RotationDegrees">The same turn in degrees.</param>
/// <param name="MaxHiLoMm">Worst radial step around the joint at that turn.</param>
public readonly record struct Alignment(int ShiftSteps, double RotationDegrees, double MaxHiLoMm);

/// <summary>
/// Hi-lo is the radial step between two pipe ends at a joint. This model keeps both ends
/// centred on the same axis and only lets the second end rotate, which is a simplification
/// of a real line-up clamp.
/// </summary>
public static class HiLo
{
    /// <summary>Hi-lo at every sample angle when <paramref name="rotating"/> is turned by <paramref name="shiftSteps"/>.</summary>
    public static double[] AroundJoint(RadiusProfile fixedEnd, RadiusProfile rotating, int shiftSteps)
    {
        EnsureSameSampling(fixedEnd, rotating);
        var n = fixedEnd.Count;
        var result = new double[n];
        for (var i = 0; i < n; i++)
        {
            result[i] = Math.Abs(fixedEnd[i] - rotating[(i + shiftSteps) % n]);
        }

        return result;
    }

    /// <summary>
    /// Tries every rotation and keeps the one with the smallest worst-case hi-lo (a minimax problem).
    /// Brute force is O(n²) per joint; the early exit skips a rotation as soon as it is
    /// already worse than the best one found, which prunes most of the work in practice.
    /// </summary>
    public static Alignment BestAlignment(RadiusProfile fixedEnd, RadiusProfile rotating)
    {
        EnsureSameSampling(fixedEnd, rotating);
        var a = fixedEnd.Radii;
        var b = rotating.Radii;
        var n = a.Length;

        var best = double.PositiveInfinity;
        var bestShift = 0;
        for (var shift = 0; shift < n; shift++)
        {
            var worst = 0.0;
            var j = shift;
            for (var i = 0; i < n; i++)
            {
                var step = Math.Abs(a[i] - b[j]);
                if (step > worst)
                {
                    worst = step;
                    if (worst >= best)
                    {
                        break;
                    }
                }

                if (++j == n)
                {
                    j = 0;
                }
            }

            if (worst < best)
            {
                best = worst;
                bestShift = shift;
            }
        }

        return new Alignment(bestShift, bestShift * 360.0 / n, best);
    }

    private static void EnsureSameSampling(RadiusProfile first, RadiusProfile second)
    {
        if (first.Count != second.Count)
        {
            throw new ArgumentException("Both ends must be sampled at the same number of angles.");
        }
    }
}
