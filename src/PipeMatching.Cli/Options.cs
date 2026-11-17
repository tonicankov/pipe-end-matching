using System.Globalization;

namespace PipeMatching.Cli;

internal sealed record Options(int Pipes, int Samples, int Seed, double LimitMm, string OutputDirectory)
{
    public static Options Parse(string[] args)
    {
        var options = new Options(Pipes: 24, Samples: 360, Seed: 42, LimitMm: 1.5, OutputDirectory: "results");

        for (var i = 0; i < args.Length - 1; i += 2)
        {
            var value = args[i + 1];
            options = args[i] switch
            {
                "--pipes" => options with { Pipes = int.Parse(value, CultureInfo.InvariantCulture) },
                "--samples" => options with { Samples = int.Parse(value, CultureInfo.InvariantCulture) },
                "--seed" => options with { Seed = int.Parse(value, CultureInfo.InvariantCulture) },
                "--limit" => options with { LimitMm = double.Parse(value, CultureInfo.InvariantCulture) },
                "--out" => options with { OutputDirectory = value },
                _ => throw new ArgumentException($"Unknown option {args[i]}"),
            };
        }

        if (options.Pipes < 2 || options.Samples < 4 || options.Samples % 2 != 0)
        {
            throw new ArgumentException("Need at least 2 pipes and an even number of samples (4 or more).");
        }

        return options;
    }
}
