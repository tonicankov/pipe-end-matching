namespace PipeMatching.Core;

/// <summary>Shape of the synthetic pipes. Defaults are illustrative, not taken from any standard.</summary>
public sealed record GeneratorSettings(
    int SamplesPerEnd = 360,
    double NominalInnerRadiusMm = 285.0,
    double PipeRadiusSpreadMm = 0.4,
    double EndRadiusSpreadMm = 0.15,
    double MaxOvalAmplitudeMm = 1.5,
    double MaxTriangularAmplitudeMm = 0.3,
    double MeasurementNoiseMm = 0.05);

/// <summary>
/// Makes fake measured pipes. Each end gets: the pipe's own radius (manufacturing spread),
/// a small end-specific offset, an oval component (cos 2φ), a three-lobed component (cos 3φ)
/// and Gaussian measurement noise.
/// </summary>
public static class PipeGenerator
{
    public static List<Pipe> Generate(int count, int seed, GeneratorSettings? settings = null)
    {
        settings ??= new GeneratorSettings();
        var random = new Random(seed);

        var pipes = new List<Pipe>(count);
        for (var id = 0; id < count; id++)
        {
            var pipeRadius = settings.NominalInnerRadiusMm + Gaussian(random) * settings.PipeRadiusSpreadMm;
            pipes.Add(new Pipe(id, GenerateEnd(random, settings, pipeRadius), GenerateEnd(random, settings, pipeRadius)));
        }

        return pipes;
    }

    private static RadiusProfile GenerateEnd(Random random, GeneratorSettings settings, double pipeRadius)
    {
        var baseRadius = pipeRadius + Gaussian(random) * settings.EndRadiusSpreadMm;
        var ovalAmplitude = random.NextDouble() * settings.MaxOvalAmplitudeMm;
        var ovalPhase = random.NextDouble() * Math.PI;
        var triangularAmplitude = random.NextDouble() * settings.MaxTriangularAmplitudeMm;
        var triangularPhase = random.NextDouble() * 2 * Math.PI / 3;

        var radii = new double[settings.SamplesPerEnd];
        for (var i = 0; i < radii.Length; i++)
        {
            var angle = 2 * Math.PI * i / radii.Length;
            radii[i] = baseRadius
                + ovalAmplitude * Math.Cos(2 * (angle - ovalPhase))
                + triangularAmplitude * Math.Cos(3 * (angle - triangularPhase))
                + Gaussian(random) * settings.MeasurementNoiseMm;
        }

        return new RadiusProfile(radii);
    }

    /// <summary>Standard normal sample (Box–Muller).</summary>
    private static double Gaussian(Random random)
    {
        var u1 = 1.0 - random.NextDouble(); // avoid log(0)
        var u2 = random.NextDouble();
        return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
    }
}
