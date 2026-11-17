# Pipe End Matching

A small C# / .NET 8 practice project: given measured pipe ends, find the order, orientation and
rotation of the pipes in a pipeline string so the **hi-lo** (radial misalignment) at every
weld joint stays as small as possible.

This is my own model of the problem, built to understand it. It is not how any real system
works, and the numbers are synthetic.

## Run it

```bash
dotnet test
dotnet run --project src/PipeMatching.Cli -- --pipes 24 --samples 360 --seed 42 --limit 1.5
```

All options are optional. Output goes to `results/`:

- `joints.csv`: every joint in the optimized string with rotation and hi-lo
- `worst-joint.svg`: polar plot of the worst joint (open it in a browser)

The console prints a comparison like this (illustrative numbers; yours depend on the seed):

```
Strategy        Over limit  Worst (mm)   Mean (mm)
As delivered             4       1.845       1.139
Greedy                   0       0.754       0.448
Optimized                0       0.686       0.438
```

## How it works

1. **Synthetic pipes** (`PipeGenerator`). Each end is an inner radius sampled at N angles:
   pipe radius spread + end offset + oval shape (cos 2φ) + three-lobed shape (cos 3φ) + measurement noise.
2. **One joint** (`HiLo.BestAlignment`). Keep the previous pipe's end fixed and try every rotation of the
   incoming end. For each rotation, hi-lo is the worst |r₁(φ) − r₂(φ + θ)| around the circumference.
   Keep the rotation with the smallest worst case (a minimax problem). The inner loop stops as soon as a
   rotation is already worse than the best one, which prunes most of the O(n²) work.
3. **Orientation** (`PlacedPipe`). A pipe can be laid either way round. Flipping it swaps its ends **and**
   mirrors the angles (its axis now points backwards), so a flipped end is `Mirrored()`.
4. **All joints at once** (`CostMatrix`). Because the incoming pipe can always be turned freely, the best
   hi-lo of a joint depends only on the two ends, not on earlier rotations. So every
   (pipe, orientation) → (pipe, orientation) joint is computed once, in parallel.
5. **The string** (`SequenceOptimizer`). Choosing order + orientation is close to a travelling-salesman
   path problem, so it uses heuristics:
   - greedy: start from every pipe/orientation, always attach the best remaining one
   - local search: 2-opt (reverse a stretch of the string, which also flips every pipe in it) and swaps,
     until nothing improves
   - objective, in order: joints over the limit, worst joint, total hi-lo

## Things I found interesting

- **Ovality misses triangular shapes.** A three-lobed end has a constant diameter, so a diameter gauge
  says it is perfect, but it still causes hi-lo against a round pipe. There is a test for this.
- **Mirroring matters.** Forgetting that a flipped pipe's angles run backwards gives wrong rotations,
  and only shows up with asymmetric shapes.
- **Reversal symmetry.** A joint scores the same when the whole string is laid the other way round,
  so half the cost matrix is redundant. The test proves it; the code does not use it yet.
- **Locale.** CSV and SVG are written with the invariant culture, otherwise a Dutch or Bulgarian
  machine writes `1,5` and breaks the files.

## Simplifications and next steps

- Ends are only rotated, never shifted. A real line-up clamp can also offset the centres, which turns
  this into a 2D minimax problem (smallest enclosing annulus style).
- Free rotation is unrealistic. Seam position limits (e.g. longitudinal seam near the top) couple the
  rotation of consecutive joints, so joints are no longer independent: dynamic programming over rotations.
- Faster joint search: coarse-to-fine rotation search, or FFT cross-correlation on a least-squares cost.
- Use the reversal symmetry to halve the cost matrix.
- Real measurements: outliers from the laser, filtering, missing samples, different sample counts.
- A small WPF or Blazor viewer instead of the SVG.
- Compare the heuristic against an exact solver on small instances to see how far from optimal it is.

## Layout

```
src/PipeMatching.Core    geometry, generator, cost matrix, optimizer
src/PipeMatching.Cli     console app, CSV and SVG output
tests/PipeMatching.Tests xUnit tests for the geometry and the optimizer
```
