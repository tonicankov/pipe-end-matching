namespace PipeMatching.Core;

/// <summary>
/// Inner radius of one pipe end in millimetres, sampled at equally spaced angles.
/// Sample i sits at angle i * 360 / Count degrees. Angles run counter-clockwise
/// around the pipe axis when looking along the axis from end A towards end B.
/// </summary>
public sealed class RadiusProfile
{
    private readonly double[] _radii;

    public RadiusProfile(IEnumerable<double> radiiMm)
    {
        _radii = radiiMm.ToArray();
        if (_radii.Length < 4)
        {
            throw new ArgumentException("A profile needs at least 4 samples.", nameof(radiiMm));
        }
    }

    public int Count => _radii.Length;

    public double AngleStepDegrees => 360.0 / _radii.Length;

    public double this[int index] => _radii[index];

    public ReadOnlySpan<double> Radii => _radii;

    public double MeanRadius => _radii.Average();

    /// <summary>
    /// Largest minus smallest diameter, where each diameter is measured across opposite samples.
    /// Note: a three-lobed (triangular) shape has a constant diameter, so ovality misses it,
    /// while hi-lo against another pipe does not.
    /// </summary>
    public double Ovality
    {
        get
        {
            if (_radii.Length % 2 != 0)
            {
                throw new InvalidOperationException("Ovality needs an even number of samples.");
            }

            var half = _radii.Length / 2;
            var min = double.PositiveInfinity;
            var max = double.NegativeInfinity;
            for (var i = 0; i < half; i++)
            {
                var diameter = _radii[i] + _radii[i + half];
                min = Math.Min(min, diameter);
                max = Math.Max(max, diameter);
            }

            return max - min;
        }
    }

    /// <summary>
    /// The same end described with the angle direction reversed: r'(φ) = r(−φ).
    /// Needed when a pipe is laid the other way round, because its axis then points backwards.
    /// </summary>
    public RadiusProfile Mirrored()
    {
        var n = _radii.Length;
        var mirrored = new double[n];
        for (var i = 0; i < n; i++)
        {
            mirrored[i] = _radii[(n - i) % n];
        }

        return new RadiusProfile(mirrored);
    }

    /// <summary>
    /// The profile turned by <paramref name="steps"/> samples: r'(i) = r(i − steps).
    /// </summary>
    public RadiusProfile Rotated(int steps)
    {
        var n = _radii.Length;
        var shift = ((steps % n) + n) % n;
        var rotated = new double[n];
        for (var i = 0; i < n; i++)
        {
            rotated[i] = _radii[(i - shift + n) % n];
        }

        return new RadiusProfile(rotated);
    }
}
