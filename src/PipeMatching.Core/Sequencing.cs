namespace PipeMatching.Core;

/// <summary>
/// Quality of a pipe string. Compared in order: joints over the limit, then the worst joint,
/// then the total hi-lo as a tie-breaker.
/// </summary>
public readonly record struct Score(int JointsOverLimit, double MaxHiLoMm, double TotalHiLoMm)
{
    private const double Tolerance = 1e-9;

    public bool IsBetterThan(Score other)
    {
        if (JointsOverLimit != other.JointsOverLimit)
        {
            return JointsOverLimit < other.JointsOverLimit;
        }

        if (Math.Abs(MaxHiLoMm - other.MaxHiLoMm) > Tolerance)
        {
            return MaxHiLoMm < other.MaxHiLoMm;
        }

        return TotalHiLoMm < other.TotalHiLoMm - Tolerance;
    }
}

public static class Sequences
{
    /// <summary>Pipes in delivery order, none flipped: the baseline to beat.</summary>
    public static PlacedPipe[] AsDelivered(int pipeCount) =>
        Enumerable.Range(0, pipeCount).Select(i => new PlacedPipe(i, false)).ToArray();

    public static Score Evaluate(IReadOnlyList<PlacedPipe> sequence, CostMatrix costs, double limitMm)
    {
        var over = 0;
        var max = 0.0;
        var total = 0.0;
        for (var i = 1; i < sequence.Count; i++)
        {
            var hiLo = costs.HiLoMm(sequence[i - 1], sequence[i]);
            if (hiLo > limitMm)
            {
                over++;
            }

            max = Math.Max(max, hiLo);
            total += hiLo;
        }

        return new Score(over, max, total);
    }

    /// <summary>The whole string laid in the opposite direction. Its joints are the same, so its score is too.</summary>
    public static PlacedPipe[] Reversed(IReadOnlyList<PlacedPipe> sequence) =>
        sequence.Reverse().Select(p => p.Turned()).ToArray();
}

/// <summary>
/// Chooses the order and orientation of the pipes. This is close to a travelling-salesman path
/// problem, so it uses a heuristic: greedy construction, then local search. It finds good
/// strings, not provably optimal ones.
/// </summary>
public sealed class SequenceOptimizer(CostMatrix costs, double limitMm)
{
    /// <summary>Best result from greedy starts and from the delivered order, each improved by local search.</summary>
    public PlacedPipe[] Optimize()
    {
        var candidates = new[] { BestGreedy(), Sequences.AsDelivered(costs.PipeCount) };

        PlacedPipe[]? best = null;
        var bestScore = default(Score);
        foreach (var candidate in candidates)
        {
            var improved = Improve(candidate);
            var score = Sequences.Evaluate(improved, costs, limitMm);
            if (best is null || score.IsBetterThan(bestScore))
            {
                best = improved;
                bestScore = score;
            }
        }

        return best!;
    }

    /// <summary>Greedy from every possible first pipe and orientation; keeps the best string.</summary>
    public PlacedPipe[] BestGreedy()
    {
        PlacedPipe[]? best = null;
        var bestScore = default(Score);
        for (var start = 0; start < costs.PipeCount * 2; start++)
        {
            var sequence = GreedyFrom(PlacedPipe.FromNode(start));
            var score = Sequences.Evaluate(sequence, costs, limitMm);
            if (best is null || score.IsBetterThan(bestScore))
            {
                best = sequence;
                bestScore = score;
            }
        }

        return best!;
    }

    /// <summary>Always attach the unused pipe and orientation that gives the lowest hi-lo next.</summary>
    public PlacedPipe[] GreedyFrom(PlacedPipe first)
    {
        var used = new bool[costs.PipeCount];
        var sequence = new List<PlacedPipe>(costs.PipeCount) { first };
        used[first.PipeIndex] = true;

        var current = first;
        while (sequence.Count < costs.PipeCount)
        {
            var bestNode = -1;
            var bestHiLo = double.PositiveInfinity;
            for (var node = 0; node < costs.PipeCount * 2; node++)
            {
                if (used[node / 2])
                {
                    continue;
                }

                var hiLo = costs.HiLoMm(current.Node, node);
                if (hiLo < bestHiLo)
                {
                    bestHiLo = hiLo;
                    bestNode = node;
                }
            }

            current = PlacedPipe.FromNode(bestNode);
            sequence.Add(current);
            used[current.PipeIndex] = true;
        }

        return sequence.ToArray();
    }

    /// <summary>
    /// Local search with two moves until neither helps:
    /// 2-opt (reverse a stretch of the string, which also turns every pipe in it; a stretch of
    /// one pipe is a single flip) and swapping two pipes.
    /// </summary>
    public PlacedPipe[] Improve(IReadOnlyList<PlacedPipe> start)
    {
        var current = start.ToArray();
        var currentScore = Sequences.Evaluate(current, costs, limitMm);
        var n = current.Length;

        var improved = true;
        while (improved)
        {
            improved = false;

            for (var i = 0; i < n && !improved; i++)
            {
                for (var j = i; j < n && !improved; j++)
                {
                    var candidate = ReverseSegment(current, i, j);
                    var score = Sequences.Evaluate(candidate, costs, limitMm);
                    if (score.IsBetterThan(currentScore))
                    {
                        (current, currentScore, improved) = (candidate, score, true);
                    }
                }
            }

            for (var i = 0; i < n && !improved; i++)
            {
                for (var j = i + 1; j < n && !improved; j++)
                {
                    var candidate = (PlacedPipe[])current.Clone();
                    (candidate[i], candidate[j]) = (candidate[j], candidate[i]);
                    var score = Sequences.Evaluate(candidate, costs, limitMm);
                    if (score.IsBetterThan(currentScore))
                    {
                        (current, currentScore, improved) = (candidate, score, true);
                    }
                }
            }
        }

        return current;
    }

    private static PlacedPipe[] ReverseSegment(PlacedPipe[] sequence, int from, int to)
    {
        var result = (PlacedPipe[])sequence.Clone();
        for (var k = 0; k <= to - from; k++)
        {
            result[from + k] = sequence[to - k].Turned();
        }

        return result;
    }
}
