using System.Diagnostics;
using System.Globalization;
using PipeMatching.Cli;
using PipeMatching.Core;

// Usage: dotnet run --project src/PipeMatching.Cli -- --pipes 24 --samples 360 --seed 42 --limit 1.5 --out results
var options = Options.Parse(args);

Console.WriteLine($"Generating {options.Pipes} pipes, {options.Samples} samples per end, seed {options.Seed}");
var pipes = PipeGenerator.Generate(options.Pipes, options.Seed, new GeneratorSettings(SamplesPerEnd: options.Samples));

var stopwatch = Stopwatch.StartNew();
var costs = new CostMatrix(pipes);
stopwatch.Stop();
Console.WriteLine($"Cost matrix: {options.Pipes * 2}x{options.Pipes * 2} joints in {stopwatch.ElapsedMilliseconds} ms");

var optimizer = new SequenceOptimizer(costs, options.LimitMm);
var strategies = new (string Name, PlacedPipe[] Sequence)[]
{
    ("As delivered", Sequences.AsDelivered(pipes.Count)),
    ("Greedy", optimizer.BestGreedy()),
    ("Optimized", optimizer.Optimize()),
};

Console.WriteLine();
Console.WriteLine($"Hi-lo limit: {options.LimitMm:F2} mm");
Console.WriteLine($"{"Strategy",-14}{"Over limit",12}{"Worst (mm)",12}{"Mean (mm)",12}");
foreach (var (name, sequence) in strategies)
{
    var score = Sequences.Evaluate(sequence, costs, options.LimitMm);
    var mean = score.TotalHiLoMm / (sequence.Length - 1);
    Console.WriteLine($"{name,-14}{score.JointsOverLimit,12}{score.MaxHiLoMm,12:F3}{mean,12:F3}");
}

var best = strategies[^1].Sequence;
Console.WriteLine();
Console.WriteLine("Optimized string (rotation = how far to turn the incoming pipe):");
Console.WriteLine($"{"Joint",6}{"From",10}{"To",10}{"Rotate (deg)",14}{"Hi-lo (mm)",12}");
for (var i = 1; i < best.Length; i++)
{
    var hiLo = costs.HiLoMm(best[i - 1], best[i]);
    var flag = hiLo > options.LimitMm ? "  over limit" : string.Empty;
    Console.WriteLine(
        $"{i,6}{Label(best[i - 1], pipes),10}{Label(best[i], pipes),10}{costs.RotationDegrees(best[i - 1], best[i]),14:F1}{hiLo,12:F3}{flag}");
}

Directory.CreateDirectory(options.OutputDirectory);
var csvPath = Path.Combine(options.OutputDirectory, "joints.csv");
File.WriteAllText(csvPath, Report.JointsCsv(best, pipes, costs));

var worstIndex = Enumerable.Range(1, best.Length - 1).MaxBy(i => costs.HiLoMm(best[i - 1], best[i]));
var svgPath = Path.Combine(options.OutputDirectory, "worst-joint.svg");
File.WriteAllText(svgPath, Report.JointSvg(best[worstIndex - 1], best[worstIndex], pipes, costs));

Console.WriteLine();
Console.WriteLine($"Wrote {csvPath} and {svgPath}");

static string Label(PlacedPipe placed, IReadOnlyList<Pipe> pipes) =>
    string.Create(CultureInfo.InvariantCulture, $"P{pipes[placed.PipeIndex].Id:D2}{(placed.Flipped ? " (fl)" : string.Empty)}");
