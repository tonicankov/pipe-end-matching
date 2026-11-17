using PipeMatching.Core;

namespace PipeMatching.Tests;

public class GeometryTests
{
    private const int Samples = 360;

    private static RadiusProfile Shape(double radius, double oval = 0, double triangular = 0) =>
        new(Enumerable.Range(0, Samples).Select(i =>
        {
            var angle = 2 * Math.PI * i / Samples;
            return radius + oval * Math.Cos(2 * angle) + triangular * Math.Cos(3 * angle);
        }));

    [Fact]
    public void Identical_circles_have_zero_hi_lo()
    {
        var circle = Shape(285);

        Assert.Equal(0, HiLo.BestAlignment(circle, circle).MaxHiLoMm, precision: 12);
    }

    [Fact]
    public void Circles_of_different_size_have_hi_lo_equal_to_the_radius_difference()
    {
        var alignment = HiLo.BestAlignment(Shape(285), Shape(285.5));

        Assert.Equal(0.5, alignment.MaxHiLoMm, precision: 9);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(37)]
    [InlineData(359)]
    public void A_turned_copy_is_found_at_the_turn_that_undoes_it(int turnSteps)
    {
        // Noise makes the end unique, so only one rotation brings it back.
        var end = PipeGenerator.Generate(1, seed: 7)[0].EndA;
        var turned = end.Rotated(turnSteps);

        var alignment = HiLo.BestAlignment(end, turned);

        Assert.Equal(turnSteps, alignment.ShiftSteps);
        Assert.Equal(0, alignment.MaxHiLoMm, precision: 12);
    }

    [Fact]
    public void Two_ovals_at_90_degrees_line_up_after_a_quarter_turn()
    {
        var oval = Shape(285, oval: 1.0);
        var crossed = oval.Rotated(Samples / 4);

        var alignment = HiLo.BestAlignment(oval, crossed);

        Assert.Equal(0, alignment.MaxHiLoMm, precision: 12);
        Assert.Equal(90.0, alignment.RotationDegrees % 180.0, precision: 9);
    }

    [Fact]
    public void Mirroring_twice_gives_the_original()
    {
        var end = PipeGenerator.Generate(1, seed: 3)[0].EndB;

        var twice = end.Mirrored().Mirrored();

        Assert.Equal(end.Radii.ToArray(), twice.Radii.ToArray());
    }

    [Fact]
    public void Ovality_of_a_pure_oval_is_four_times_its_amplitude()
    {
        Assert.Equal(4.0, Shape(285, oval: 1.0).Ovality, precision: 9);
    }

    [Fact]
    public void A_triangular_end_has_no_ovality_but_still_gives_hi_lo()
    {
        var triangular = Shape(285, triangular: 0.3);

        Assert.Equal(0, triangular.Ovality, precision: 9);
        Assert.Equal(0.3, HiLo.BestAlignment(Shape(285), triangular).MaxHiLoMm, precision: 9);
    }
}
