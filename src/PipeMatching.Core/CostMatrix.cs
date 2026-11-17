namespace PipeMatching.Core;

/// <summary>
/// Best achievable hi-lo for every possible joint: the exit end of one placed pipe against
/// the entry end of another. Because the incoming pipe can always be turned freely, the best
/// hi-lo of a joint does not depend on how earlier pipes were turned, so it can be computed once.
/// </summary>
public sealed class CostMatrix
{
    private readonly double[,] _hiLoMm;
    private readonly int[,] _shiftSteps;

    public CostMatrix(IReadOnlyList<Pipe> pipes)
    {
        if (pipes.Count < 2)
        {
            throw new ArgumentException("Need at least two pipes.", nameof(pipes));
        }

        PipeCount = pipes.Count;
        SamplesPerEnd = pipes[0].EndA.Count;

        var nodeCount = PipeCount * 2;
        var exits = new RadiusProfile[nodeCount];
        var entries = new RadiusProfile[nodeCount];
        for (var node = 0; node < nodeCount; node++)
        {
            var placed = PlacedPipe.FromNode(node);
            exits[node] = placed.ExitEnd(pipes[placed.PipeIndex]);
            entries[node] = placed.EntryEnd(pipes[placed.PipeIndex]);
        }

        var hiLo = new double[nodeCount, nodeCount];
        var shifts = new int[nodeCount, nodeCount];

        // Each row is independent, so rows run in parallel.
        Parallel.For(0, nodeCount, from =>
        {
            for (var to = 0; to < nodeCount; to++)
            {
                if (from / 2 == to / 2)
                {
                    hiLo[from, to] = double.PositiveInfinity; // a pipe cannot follow itself
                    continue;
                }

                var alignment = HiLo.BestAlignment(exits[from], entries[to]);
                hiLo[from, to] = alignment.MaxHiLoMm;
                shifts[from, to] = alignment.ShiftSteps;
            }
        });

        _hiLoMm = hiLo;
        _shiftSteps = shifts;
    }

    public int PipeCount { get; }

    public int SamplesPerEnd { get; }

    public static int Node(int pipeIndex, bool flipped) => pipeIndex * 2 + (flipped ? 1 : 0);

    public double HiLoMm(PlacedPipe from, PlacedPipe to) => _hiLoMm[from.Node, to.Node];

    public double HiLoMm(int fromNode, int toNode) => _hiLoMm[fromNode, toNode];

    /// <summary>How far the incoming pipe must be turned, in degrees, to reach the best hi-lo.</summary>
    public double RotationDegrees(PlacedPipe from, PlacedPipe to) =>
        _shiftSteps[from.Node, to.Node] * 360.0 / SamplesPerEnd;

    public int ShiftSteps(PlacedPipe from, PlacedPipe to) => _shiftSteps[from.Node, to.Node];
}
