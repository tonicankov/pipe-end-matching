namespace PipeMatching.Core;

/// <summary>A pipe joint with a measured profile at each end.</summary>
public sealed record Pipe(int Id, RadiusProfile EndA, RadiusProfile EndB);

/// <summary>
/// A pipe at a position in the string. Not flipped: end A faces the previous pipe.
/// Flipped: the pipe is laid the other way round, so end B faces the previous pipe.
/// </summary>
public readonly record struct PlacedPipe(int PipeIndex, bool Flipped)
{
    /// <summary>Index into the cost matrix: two nodes per pipe, one per orientation.</summary>
    public int Node => CostMatrix.Node(PipeIndex, Flipped);

    public PlacedPipe Turned() => this with { Flipped = !Flipped };

    public static PlacedPipe FromNode(int node) => new(node / 2, node % 2 == 1);

    /// <summary>The end that meets the previous pipe, in pipeline coordinates.</summary>
    public RadiusProfile EntryEnd(Pipe pipe) => Flipped ? pipe.EndB.Mirrored() : pipe.EndA;

    /// <summary>The end that meets the next pipe, in pipeline coordinates.</summary>
    public RadiusProfile ExitEnd(Pipe pipe) => Flipped ? pipe.EndA.Mirrored() : pipe.EndB;
}
