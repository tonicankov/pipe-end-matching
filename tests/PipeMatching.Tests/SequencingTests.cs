using PipeMatching.Core;

namespace PipeMatching.Tests;

public class SequencingTests
{
    private const double Limit = 1.5;

    private static (List<Pipe> Pipes, CostMatrix Costs) Setup(int pipes = 12, int samples = 120, int seed = 42)
    {
        var generated = PipeGenerator.Generate(pipes, seed, new GeneratorSettings(SamplesPerEnd: samples));
        return (generated, new CostMatrix(generated));
    }

    [Fact]
    public void A_joint_scores_the_same_when_the_whole_string_is_laid_the_other_way()
    {
        var (pipes, costs) = Setup();

        for (var from = 0; from < pipes.Count * 2; from++)
        {
            for (var to = 0; to < pipes.Count * 2; to++)
            {
                if (from / 2 == to / 2)
                {
                    continue;
                }

                var a = PlacedPipe.FromNode(from);
                var b = PlacedPipe.FromNode(to);
                Assert.Equal(costs.HiLoMm(a, b), costs.HiLoMm(b.Turned(), a.Turned()), precision: 9);
            }
        }
    }

    [Fact]
    public void Reversing_a_string_keeps_its_score()
    {
        var (pipes, costs) = Setup();
        var sequence = new SequenceOptimizer(costs, Limit).BestGreedy();

        var forward = Sequences.Evaluate(sequence, costs, Limit);
        var backward = Sequences.Evaluate(Sequences.Reversed(sequence), costs, Limit);

        Assert.Equal(forward.MaxHiLoMm, backward.MaxHiLoMm, precision: 9);
        Assert.Equal(forward.TotalHiLoMm, backward.TotalHiLoMm, precision: 9);
        Assert.Equal(pipes.Count, sequence.Length);
    }

    [Fact]
    public void Greedy_uses_every_pipe_exactly_once()
    {
        var (pipes, costs) = Setup();

        var sequence = new SequenceOptimizer(costs, Limit).GreedyFrom(new PlacedPipe(5, Flipped: true));

        Assert.Equal(Enumerable.Range(0, pipes.Count), sequence.Select(p => p.PipeIndex).Order());
        Assert.Equal(new PlacedPipe(5, true), sequence[0]);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void Optimized_string_is_never_worse_than_delivery_order_or_greedy(int seed)
    {
        var (pipes, costs) = Setup(seed: seed);
        var optimizer = new SequenceOptimizer(costs, Limit);

        var optimized = optimizer.Optimize();
        var optimizedScore = Sequences.Evaluate(optimized, costs, Limit);

        Assert.Equal(Enumerable.Range(0, pipes.Count), optimized.Select(p => p.PipeIndex).Order());
        Assert.False(Sequences.Evaluate(Sequences.AsDelivered(pipes.Count), costs, Limit).IsBetterThan(optimizedScore));
        Assert.False(Sequences.Evaluate(optimizer.BestGreedy(), costs, Limit).IsBetterThan(optimizedScore));
    }
}
